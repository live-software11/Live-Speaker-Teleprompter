const { app } = require('electron');
const path = require('path');
const fs = require('fs');

class NDIHandler {
    constructor(mainWindow) {
        this.mainWindow = mainWindow;
        this.ndiSender = null;
        this.isActive = false;
        this.sourceName = 'R-Speaker Teleprompter';
        this.frameRate = 30;
        this.resolution = { width: 1920, height: 1080 };
        
        try {
            // Try to load NDI SDK if available
            const ndiPath = path.join(app.getPath('userData'), 'ndi-sdk');
            if (fs.existsSync(ndiPath)) {
                this.ndiLib = require(path.join(ndiPath, 'ndi-lib'));
                this.isNDIAvailable = true;
                console.log('NDI SDK loaded successfully');
            } else {
                console.log('NDI SDK not found - NDI output disabled');
                this.isNDIAvailable = false;
            }
        } catch (error) {
            console.error('Failed to load NDI SDK:', error);
            this.isNDIAvailable = false;
        }
    }

    initialize() {
        if (!this.isNDIAvailable) {
            console.log('NDI not available - skipping initialization');
            return false;
        }

        try {
            // Initialize NDI
            if (!this.ndiLib.initialize()) {
                console.error('Failed to initialize NDI');
                return false;
            }

            // Create NDI sender
            const senderSettings = {
                sourceName: this.sourceName,
                groups: null,
                clockVideo: true,
                clockAudio: false
            };

            this.ndiSender = this.ndiLib.createSender(senderSettings);
            if (!this.ndiSender) {
                console.error('Failed to create NDI sender');
                return false;
            }

            console.log(`NDI sender created: ${this.sourceName}`);
            return true;
        } catch (error) {
            console.error('NDI initialization error:', error);
            return false;
        }
    }

    start(config = {}) {
        if (!this.isNDIAvailable || !this.ndiSender) {
            console.log('NDI not available or not initialized');
            return false;
        }

        try {
            // Apply configuration
            if (config.sourceName) this.sourceName = config.sourceName;
            if (config.frameRate) this.frameRate = config.frameRate;
            if (config.resolution) this.resolution = config.resolution;

            // Start capturing from window
            this.startCapture();
            this.isActive = true;
            
            // Notify renderer
            this.mainWindow.webContents.send('ndi-status', { 
                active: true, 
                sourceName: this.sourceName,
                resolution: this.resolution,
                frameRate: this.frameRate
            });

            console.log('NDI output started');
            return true;
        } catch (error) {
            console.error('Failed to start NDI output:', error);
            return false;
        }
    }

    startCapture() {
        if (!this.captureInterval) {
            this.captureInterval = setInterval(() => {
                this.captureAndSend();
            }, 1000 / this.frameRate);
        }
    }

    async captureAndSend() {
        if (!this.isActive || !this.ndiSender) return;

        try {
            // Capture the teleprompter content
            const image = await this.mainWindow.webContents.capturePage({
                x: 0,
                y: 0,
                width: this.resolution.width,
                height: this.resolution.height
            });

            if (!image.isEmpty()) {
                // Convert to BGRA format for NDI
                const buffer = image.toBitmap();
                
                // Create NDI video frame
                const videoFrame = {
                    width: this.resolution.width,
                    height: this.resolution.height,
                    fourCC: 'BGRA',
                    frameRateN: this.frameRate,
                    frameRateD: 1,
                    aspectRatio: this.resolution.width / this.resolution.height,
                    data: buffer,
                    lineStride: this.resolution.width * 4
                };

                // Send frame via NDI
                this.ndiSender.sendVideo(videoFrame);
            }
        } catch (error) {
            console.error('Frame capture error:', error);
        }
    }

    stop() {
        if (this.captureInterval) {
            clearInterval(this.captureInterval);
            this.captureInterval = null;
        }

        this.isActive = false;

        if (this.ndiSender) {
            try {
                this.ndiSender.destroy();
                console.log('NDI output stopped');
            } catch (error) {
                console.error('Error stopping NDI:', error);
            }
        }

        // Notify renderer
        this.mainWindow.webContents.send('ndi-status', { 
            active: false 
        });
    }

    setResolution(width, height) {
        this.resolution = { width, height };
        if (this.isActive) {
            this.stop();
            this.start();
        }
    }

    setFrameRate(fps) {
        this.frameRate = fps;
        if (this.captureInterval) {
            clearInterval(this.captureInterval);
            this.startCapture();
        }
    }

    setSourceName(name) {
        this.sourceName = name;
        if (this.isActive) {
            this.stop();
            this.initialize();
            this.start();
        }
    }

    getStatus() {
        return {
            available: this.isNDIAvailable,
            active: this.isActive,
            sourceName: this.sourceName,
            resolution: this.resolution,
            frameRate: this.frameRate
        };
    }

    // Alternative NDI output using OBS Virtual Camera bridge
    async setupOBSBridge() {
        try {
            // Check if OBS Virtual Camera is available
            const devices = await navigator.mediaDevices.enumerateDevices();
            const virtualCam = devices.find(device => 
                device.kind === 'videoinput' && 
                device.label.includes('OBS Virtual Camera')
            );

            if (virtualCam) {
                console.log('OBS Virtual Camera detected - can be used as NDI source');
                return true;
            }
            return false;
        } catch (error) {
            console.error('Failed to check OBS Virtual Camera:', error);
            return false;
        }
    }

    destroy() {
        this.stop();
        if (this.ndiLib) {
            this.ndiLib.destroy();
        }
    }
}

module.exports = NDIHandler;
