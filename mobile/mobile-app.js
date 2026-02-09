// Mobile App Controller
class MobileTelepromper {
    constructor() {
        this.isPlaying = false;
        this.scrollSpeed = 5;
        this.scripts = this.loadScripts();
        this.currentScript = null;
        this.scrollInterval = null;
        this.wsConnection = null;
        
        this.initElements();
        this.initEventListeners();
        this.initGestures();
        this.loadSettings();
    }
    
    initElements() {
        // Main elements
        this.teleprompterText = document.getElementById('teleprompterText');
        this.playBtn = document.getElementById('playBtn');
        this.speedValue = document.getElementById('speedValue');
        this.scrollIndicator = document.querySelector('.scroll-progress');
        
        // Menu elements
        this.sideMenu = document.getElementById('sideMenu');
        this.settingsPanel = document.getElementById('settingsPanel');
        this.scriptList = document.getElementById('scriptList');
        
        // Modals
        this.scriptEditor = document.getElementById('scriptEditor');
        this.connectionModal = document.getElementById('connectionModal');
    }
    
    initEventListeners() {
        // Play/Pause
        this.playBtn.addEventListener('click', () => this.togglePlay());
        
        // Speed controls
        document.getElementById('speedUp').addEventListener('click', () => {
            if (this.scrollSpeed < 10) {
                this.scrollSpeed++;
                this.updateSpeed();
            }
        });
        
        document.getElementById('speedDown').addEventListener('click', () => {
            if (this.scrollSpeed > 1) {
                this.scrollSpeed--;
                this.updateSpeed();
            }
        });
        
        // Reset
        document.getElementById('resetBtn').addEventListener('click', () => {
            this.reset();
        });
        
        // Menu
        document.getElementById('menuBtn').addEventListener('click', () => {
            this.sideMenu.classList.add('active');
        });
        
        document.getElementById('closeMenu').addEventListener('click', () => {
            this.sideMenu.classList.remove('active');
        });
        
        // Settings
        document.getElementById('settingsBtn').addEventListener('click', () => {
            this.settingsPanel.classList.add('active');
        });
        
        document.getElementById('closeSettings').addEventListener('click', () => {
            this.settingsPanel.classList.remove('active');
        });
        
        // Font size
        const fontSizeSlider = document.getElementById('fontSizeSlider');
        fontSizeSlider.addEventListener('input', (e) => {
            const size = e.target.value;
            this.teleprompterText.style.fontSize = size + 'px';
            document.getElementById('fontSizeValue').textContent = size + 'px';
            this.saveSettings();
        });
        
        // Line height
        const lineHeightSlider = document.getElementById('lineHeightSlider');
        lineHeightSlider.addEventListener('input', (e) => {
            const height = e.target.value;
            this.teleprompterText.style.lineHeight = height;
            document.getElementById('lineHeightValue').textContent = height;
            this.saveSettings();
        });
        
        // Mirror toggle
        document.getElementById('mirrorToggle').addEventListener('change', (e) => {
            if (e.target.checked) {
                this.teleprompterText.classList.add('mirrored');
            } else {
                this.teleprompterText.classList.remove('mirrored');
            }
            this.saveSettings();
        });
        
        // Color buttons
        document.querySelectorAll('.color-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const color = e.target.dataset.color;
                this.teleprompterText.style.color = color;
                document.querySelectorAll('.color-btn').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
                this.saveSettings();
            });
        });
        
        // Background buttons
        document.querySelectorAll('.bg-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const bg = e.target.dataset.bg;
                document.querySelector('.teleprompter-container').style.background = bg;
                document.querySelectorAll('.bg-btn').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
                this.saveSettings();
            });
        });
        
        // New script
        document.getElementById('newScriptBtn').addEventListener('click', () => {
            this.openScriptEditor();
        });
        
        // Script editor
        document.getElementById('saveScript').addEventListener('click', () => {
            this.saveScript();
        });
        
        document.getElementById('cancelEdit').addEventListener('click', () => {
            this.closeScriptEditor();
        });
        
        document.getElementById('closeEditor').addEventListener('click', () => {
            this.closeScriptEditor();
        });
        
        // Connection
        document.getElementById('connectBtn').addEventListener('click', () => {
            this.openConnectionModal();
        });
        
        document.getElementById('connectManual').addEventListener('click', () => {
            this.connectToDesktop();
        });
        
        document.getElementById('closeConnection').addEventListener('click', () => {
            this.connectionModal.classList.remove('active');
        });
        
        // Import
        document.getElementById('importBtn').addEventListener('click', () => {
            this.importScript();
        });
        
        // Update scroll indicator
        this.teleprompterText.addEventListener('scroll', () => {
            this.updateScrollIndicator();
        });
    }
    
    initGestures() {
        let touchStartY = 0;
        let touchStartTime = 0;
        
        this.teleprompterText.addEventListener('touchstart', (e) => {
            if (this.isPlaying) {
                this.togglePlay();
            }
            touchStartY = e.touches[0].clientY;
            touchStartTime = Date.now();
        });
        
        this.teleprompterText.addEventListener('touchend', (e) => {
            const touchEndY = e.changedTouches[0].clientY;
            const touchEndTime = Date.now();
            const deltaY = touchStartY - touchEndY;
            const deltaTime = touchEndTime - touchStartTime;
            
            // Double tap to play/pause
            if (deltaTime < 300 && Math.abs(deltaY) < 10) {
                if (touchEndTime - this.lastTap < 300) {
                    this.togglePlay();
                }
                this.lastTap = touchEndTime;
            }
        });
        
        // Pinch to zoom
        let initialDistance = 0;
        let initialFontSize = 32;
        
        this.teleprompterText.addEventListener('touchstart', (e) => {
            if (e.touches.length === 2) {
                initialDistance = Math.hypot(
                    e.touches[0].clientX - e.touches[1].clientX,
                    e.touches[0].clientY - e.touches[1].clientY
                );
                initialFontSize = parseInt(window.getComputedStyle(this.teleprompterText).fontSize);
            }
        });
        
        this.teleprompterText.addEventListener('touchmove', (e) => {
            if (e.touches.length === 2) {
                e.preventDefault();
                const distance = Math.hypot(
                    e.touches[0].clientX - e.touches[1].clientX,
                    e.touches[0].clientY - e.touches[1].clientY
                );
                const scale = distance / initialDistance;
                const newSize = Math.max(20, Math.min(100, initialFontSize * scale));
                this.teleprompterText.style.fontSize = newSize + 'px';
                document.getElementById('fontSizeSlider').value = newSize;
                document.getElementById('fontSizeValue').textContent = newSize + 'px';
            }
        });
        
        // Swipe gestures
        let touchStartX = 0;
        
        document.addEventListener('touchstart', (e) => {
            touchStartX = e.touches[0].clientX;
        });
        
        document.addEventListener('touchend', (e) => {
            const touchEndX = e.changedTouches[0].clientX;
            const deltaX = touchEndX - touchStartX;
            
            // Swipe right to open menu
            if (deltaX > 100 && touchStartX < 50) {
                this.sideMenu.classList.add('active');
            }
            
            // Swipe left to open settings
            if (deltaX < -100 && touchStartX > window.innerWidth - 50) {
                this.settingsPanel.classList.add('active');
            }
        });
    }
    
    togglePlay() {
        this.isPlaying = !this.isPlaying;
        
        if (this.isPlaying) {
            this.playBtn.classList.add('playing');
            this.playBtn.querySelector('.play-icon').style.display = 'none';
            this.playBtn.querySelector('.pause-icon').style.display = 'block';
            this.startScrolling();
        } else {
            this.playBtn.classList.remove('playing');
            this.playBtn.querySelector('.play-icon').style.display = 'block';
            this.playBtn.querySelector('.pause-icon').style.display = 'none';
            this.stopScrolling();
        }
    }
    
    startScrolling() {
        if (this.scrollInterval) return;
        
        this.scrollInterval = setInterval(() => {
            const scrollAmount = this.scrollSpeed * 0.5;
            this.teleprompterText.scrollTop += scrollAmount;
            
            // Stop at bottom
            if (this.teleprompterText.scrollTop >= 
                this.teleprompterText.scrollHeight - this.teleprompterText.clientHeight) {
                this.togglePlay();
            }
        }, 16); // ~60fps
    }
    
    stopScrolling() {
        if (this.scrollInterval) {
            clearInterval(this.scrollInterval);
            this.scrollInterval = null;
        }
    }
    
    reset() {
        this.teleprompterText.scrollTop = 0;
        if (this.isPlaying) {
            this.togglePlay();
        }
        this.updateScrollIndicator();
    }
    
    updateSpeed() {
        this.speedValue.textContent = this.scrollSpeed;
        this.saveSettings();
    }
    
    updateScrollIndicator() {
        const scrollPercentage = this.teleprompterText.scrollTop / 
            (this.teleprompterText.scrollHeight - this.teleprompterText.clientHeight);
        this.scrollIndicator.style.height = (scrollPercentage * 100) + '%';
    }
    
    loadScripts() {
        const saved = localStorage.getItem('telepromterScripts');
        return saved ? JSON.parse(saved) : [
            {
                id: 1,
                title: 'Welcome Script',
                content: 'Welcome to R-Speaker Teleprompter Mobile!\n\nThis is your mobile teleprompter app designed for professional presentations.\n\nFeatures:\n- Smooth scrolling\n- Adjustable speed\n- Mirror mode\n- Custom colors\n- Remote control\n- Gesture support\n\nSwipe right from the left edge to open the menu.\nSwipe left from the right edge to open settings.\nDouble tap to play/pause.\nPinch to zoom text.\n\nEnjoy your presentation!'
            }
        ];
    }
    
    renderScripts() {
        this.scriptList.innerHTML = '';
        
        this.scripts.forEach(script => {
            const item = document.createElement('div');
            item.className = 'script-item';
            if (this.currentScript && this.currentScript.id === script.id) {
                item.classList.add('active');
            }
            
            item.innerHTML = `
                <div class="script-title">${script.title}</div>
                <div class="script-preview">${script.content.substring(0, 50)}...</div>
            `;
            
            item.addEventListener('click', () => {
                this.loadScript(script);
                this.sideMenu.classList.remove('active');
            });
            
            this.scriptList.appendChild(item);
        });
    }
    
    loadScript(script) {
        this.currentScript = script;
        this.teleprompterText.innerHTML = `<p>${script.content.replace(/\n/g, '</p><p>')}</p>`;
        this.reset();
        this.renderScripts();
    }
    
    openScriptEditor(script = null) {
        this.scriptEditor.classList.add('active');
        
        if (script) {
            document.getElementById('editorTitle').textContent = 'Edit Script';
            document.getElementById('scriptTitle').value = script.title;
            document.getElementById('scriptContent').value = script.content;
        } else {
            document.getElementById('editorTitle').textContent = 'New Script';
            document.getElementById('scriptTitle').value = '';
            document.getElementById('scriptContent').value = '';
        }
        
        this.sideMenu.classList.remove('active');
    }
    
    closeScriptEditor() {
        this.scriptEditor.classList.remove('active');
    }
    
    saveScript() {
        const title = document.getElementById('scriptTitle').value.trim();
        const content = document.getElementById('scriptContent').value.trim();
        
        if (!title || !content) {
            alert('Please enter both title and content');
            return;
        }
        
        const script = {
            id: Date.now(),
            title: title,
            content: content
        };
        
        this.scripts.push(script);
        this.saveScriptsToStorage();
        this.renderScripts();
        this.loadScript(script);
        this.closeScriptEditor();
    }
    
    saveScriptsToStorage() {
        localStorage.setItem('telepromterScripts', JSON.stringify(this.scripts));
    }
    
    importScript() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.txt,.doc,.docx';
        
        input.onchange = (e) => {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    const content = e.target.result;
                    const script = {
                        id: Date.now(),
                        title: file.name.replace(/\.[^/.]+$/, ''),
                        content: content
                    };
                    this.scripts.push(script);
                    this.saveScriptsToStorage();
                    this.renderScripts();
                    this.loadScript(script);
                };
                reader.readAsText(file);
            }
        };
        
        input.click();
        this.sideMenu.classList.remove('active');
    }
    
    openConnectionModal() {
        this.connectionModal.classList.add('active');
        this.settingsPanel.classList.remove('active');
        // Generate QR code here if needed
    }
    
    connectToDesktop() {
        const ip = document.getElementById('ipInput').value;
        const port = document.getElementById('portInput').value || '8080';
        
        if (!ip) {
            alert('Please enter IP address');
            return;
        }
        
        this.establishWebSocketConnection(`ws://${ip}:${port}`);
    }
    
    establishWebSocketConnection(url) {
        try {
            this.wsConnection = new WebSocket(url);
            
            this.wsConnection.onopen = () => {
                console.log('Connected to desktop');
                document.querySelector('.status-dot').classList.add('connected');
                document.querySelector('.status-text').textContent = 'Connected';
                this.connectionModal.classList.remove('active');
            };
            
            this.wsConnection.onmessage = (event) => {
                const data = JSON.parse(event.data);
                this.handleRemoteCommand(data);
            };
            
            this.wsConnection.onerror = (error) => {
                console.error('Connection error:', error);
                alert('Failed to connect. Please check IP and port.');
            };
            
            this.wsConnection.onclose = () => {
                document.querySelector('.status-dot').classList.remove('connected');
                document.querySelector('.status-text').textContent = 'Disconnected';
            };
        } catch (error) {
            console.error('WebSocket error:', error);
            alert('Connection failed');
        }
    }
    
    handleRemoteCommand(data) {
        switch(data.command) {
            case 'play':
                if (!this.isPlaying) this.togglePlay();
                break;
            case 'pause':
                if (this.isPlaying) this.togglePlay();
                break;
            case 'reset':
                this.reset();
                break;
            case 'speed':
                this.scrollSpeed = data.value;
                this.updateSpeed();
                break;
            case 'loadScript':
                const script = this.scripts.find(s => s.id === data.scriptId);
                if (script) this.loadScript(script);
                break;
        }
    }
    
    saveSettings() {
        const settings = {
            fontSize: document.getElementById('fontSizeSlider').value,
            lineHeight: document.getElementById('lineHeightSlider').value,
            textColor: this.teleprompterText.style.color,
            backgroundColor: document.querySelector('.teleprompter-container').style.background,
            isMirrored: this.teleprompterText.classList.contains('mirrored'),
            scrollSpeed: this.scrollSpeed
        };
        
        localStorage.setItem('teleprompterSettings', JSON.stringify(settings));
    }
    
    loadSettings() {
        const saved = localStorage.getItem('teleprompterSettings');
        if (saved) {
            const settings = JSON.parse(saved);
            
            // Apply settings
            if (settings.fontSize) {
                document.getElementById('fontSizeSlider').value = settings.fontSize;
                this.teleprompterText.style.fontSize = settings.fontSize + 'px';
                document.getElementById('fontSizeValue').textContent = settings.fontSize + 'px';
            }
            
            if (settings.lineHeight) {
                document.getElementById('lineHeightSlider').value = settings.lineHeight;
                this.teleprompterText.style.lineHeight = settings.lineHeight;
                document.getElementById('lineHeightValue').textContent = settings.lineHeight;
            }
            
            if (settings.textColor) {
                this.teleprompterText.style.color = settings.textColor;
            }
            
            if (settings.backgroundColor) {
                document.querySelector('.teleprompter-container').style.background = settings.backgroundColor;
            }
            
            if (settings.isMirrored) {
                document.getElementById('mirrorToggle').checked = true;
                this.teleprompterText.classList.add('mirrored');
            }
            
            if (settings.scrollSpeed) {
                this.scrollSpeed = settings.scrollSpeed;
                this.updateSpeed();
            }
        }
        
        // Render initial scripts
        this.renderScripts();
        if (this.scripts.length > 0) {
            this.loadScript(this.scripts[0]);
        }
    }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    const app = new MobileTelepromper();
    
    // Register service worker for PWA
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('sw.js')
            .then(registration => console.log('ServiceWorker registered'))
            .catch(error => console.log('ServiceWorker registration failed:', error));
    }
    
    // Prevent default touch behaviors
    document.addEventListener('touchmove', (e) => {
        if (e.target.closest('.teleprompter-text')) return;
        e.preventDefault();
    }, { passive: false });
    
    // Handle orientation change
    window.addEventListener('orientationchange', () => {
        setTimeout(() => {
            app.updateScrollIndicator();
        }, 100);
    });
});
