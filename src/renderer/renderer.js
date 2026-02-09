// Add OSC event listeners after DOMContentLoaded
document.addEventListener('DOMContentLoaded', () => {
    // OSC Control Listeners
    window.electronAPI.on('teleprompter-control', (event, command) => {
        switch(command) {
            case 'start':
                startTeleprompter();
                break;
            case 'stop':
                stopTeleprompter();
                break;
            case 'reset':
                resetTeleprompter();
                break;
        }
    });
    
    window.electronAPI.on('teleprompter-speed', (event, speed) => {
        speedSlider.value = speed;
        scrollSpeed = speed;
        speedValue.textContent = speed;
    });
    
    window.electronAPI.on('teleprompter-speed-adjust', (event, direction) => {
        const currentSpeed = parseInt(speedSlider.value);
        if (direction === 'increase' && currentSpeed < 10) {
            speedSlider.value = currentSpeed + 1;
        } else if (direction === 'decrease' && currentSpeed > 1) {
            speedSlider.value = currentSpeed - 1;
        }
        scrollSpeed = parseInt(speedSlider.value);
        speedValue.textContent = speedSlider.value;
    });
    
    window.electronAPI.on('teleprompter-font-size', (event, size) => {
        fontSizeSlider.value = size;
        teleprompterText.style.fontSize = size + 'px';
        fontSizeValue.textContent = size + 'px';
    });
    
    window.electronAPI.on('teleprompter-font-adjust', (event, direction) => {
        const currentSize = parseInt(fontSizeSlider.value);
        if (direction === 'increase' && currentSize < 200) {
            fontSizeSlider.value = currentSize + 5;
        } else if (direction === 'decrease' && currentSize > 20) {
            fontSizeSlider.value = currentSize - 5;
        }
        teleprompterText.style.fontSize = fontSizeSlider.value + 'px';
        fontSizeValue.textContent = fontSizeSlider.value + 'px';
    });
    
    window.electronAPI.on('teleprompter-script', (event, direction) => {
        const scripts = scriptList.querySelectorAll('.script-item');
        const currentIndex = Array.from(scripts).findIndex(s => s.classList.contains('active'));
        
        if (direction === 'next' && currentIndex < scripts.length - 1) {
            scripts[currentIndex + 1].click();
        } else if (direction === 'previous' && currentIndex > 0) {
            scripts[currentIndex - 1].click();
        }
    });
    
    window.electronAPI.on('teleprompter-script-load', (event, index) => {
        const scripts = scriptList.querySelectorAll('.script-item');
        if (scripts[index]) {
            scripts[index].click();
        }
    });
    
    window.electronAPI.on('teleprompter-position', (event, position) => {
        const maxScroll = teleprompterText.scrollHeight - teleprompterText.clientHeight;
        teleprompterText.scrollTop = maxScroll * position;
    });
    
    window.electronAPI.on('teleprompter-jump', (event, position) => {
        if (position === 'top') {
            teleprompterText.scrollTop = 0;
        } else if (position === 'bottom') {
            teleprompterText.scrollTop = teleprompterText.scrollHeight;
        }
    });
    
    window.electronAPI.on('teleprompter-mirror', (event, enabled) => {
        if (enabled) {
            teleprompterText.style.transform = 'scaleX(-1)';
        } else {
            teleprompterText.style.transform = 'scaleX(1)';
        }
    });
    
    window.electronAPI.on('teleprompter-mirror-toggle', () => {
        const currentTransform = teleprompterText.style.transform;
        if (currentTransform === 'scaleX(-1)') {
            teleprompterText.style.transform = 'scaleX(1)';
        } else {
            teleprompterText.style.transform = 'scaleX(-1)';
        }
    });
    
    window.electronAPI.on('teleprompter-status-request', () => {
        // Send current status back via IPC
        window.electronAPI.send('teleprompter-status', {
            isPlaying: isPlaying,
            speed: scrollSpeed,
            fontSize: parseInt(fontSizeSlider.value),
            position: teleprompterText.scrollTop / (teleprompterText.scrollHeight - teleprompterText.clientHeight),
            isMirrored: teleprompterText.style.transform === 'scaleX(-1)'
        });
    });
    
    // NDI Control UI
    const ndiControls = document.createElement('div');
    ndiControls.className = 'ndi-controls';
    ndiControls.innerHTML = `
        <h3>NDI Output</h3>
        <button id="ndi-toggle" class="btn btn-ndi">Start NDI</button>
        <div class="ndi-status">
            <span id="ndi-status-text">NDI: Inactive</span>
            <span id="ndi-source-name"></span>
        </div>
        <div class="ndi-settings">
            <select id="ndi-resolution">
                <option value="1920x1080">1920x1080 (Full HD)</option>
                <option value="1280x720">1280x720 (HD)</option>
                <option value="3840x2160">3840x2160 (4K)</option>
            </select>
            <select id="ndi-framerate">
                <option value="30">30 fps</option>
                <option value="25">25 fps</option>
                <option value="60">60 fps</option>
            </select>
        </div>
    `;
    
    // Add NDI controls to settings panel
    const settingsPanel = document.querySelector('.settings-panel');
    if (settingsPanel) {
        settingsPanel.appendChild(ndiControls);
    }
    
    // NDI Toggle Button
    const ndiToggle = document.getElementById('ndi-toggle');
    if (ndiToggle) {
        ndiToggle.addEventListener('click', async () => {
            const status = await window.electronAPI.invoke('ndi-status');
            if (status.active) {
                await window.electronAPI.invoke('ndi-stop');
                ndiToggle.textContent = 'Start NDI';
                ndiToggle.classList.remove('active');
            } else {
                const resolution = document.getElementById('ndi-resolution').value.split('x');
                const frameRate = parseInt(document.getElementById('ndi-framerate').value);
                
                const config = {
                    resolution: { 
                        width: parseInt(resolution[0]), 
                        height: parseInt(resolution[1]) 
                    },
                    frameRate: frameRate
                };
                
                const success = await window.electronAPI.invoke('ndi-start', config);
                if (success) {
                    ndiToggle.textContent = 'Stop NDI';
                    ndiToggle.classList.add('active');
                }
            }
        });
    }
    
    // NDI Status Updates
    window.electronAPI.on('ndi-status', (event, status) => {
        const statusText = document.getElementById('ndi-status-text');
        const sourceName = document.getElementById('ndi-source-name');
        const toggle = document.getElementById('ndi-toggle');
        
        if (status.active) {
            statusText.textContent = 'NDI: Active';
            statusText.style.color = '#4CAF50';
            sourceName.textContent = status.sourceName || '';
            if (toggle) {
                toggle.textContent = 'Stop NDI';
                toggle.classList.add('active');
            }
        } else {
            statusText.textContent = 'NDI: Inactive';
            statusText.style.color = '#999';
            sourceName.textContent = '';
            if (toggle) {
                toggle.textContent = 'Start NDI';
                toggle.classList.remove('active');
            }
        }
    });
    
    // Output mode control from OSC/Companion
    window.electronAPI.on('output-mode', (event, mode) => {
        console.log('Output mode changed to:', mode);
        // Handle different output modes
        switch(mode) {
            case 'ndi':
                // NDI only - hide local display
                document.querySelector('.teleprompter').style.opacity = '0.5';
                break;
            case 'display':
                // Display only - stop NDI
                document.querySelector('.teleprompter').style.opacity = '1';
                break;
            case 'both':
                // Both NDI and display
                document.querySelector('.teleprompter').style.opacity = '1';
                break;
        }
    });
});