```javascript
// ...existing code...

const OSCServer = require('./oscServer');
const NDIHandler = require('./ndiHandler');

let mainWindow;
let oscServer;
let ndiHandler;

// ...existing code...

function createWindow() {
  // ...existing code...

  mainWindow.on('ready-to-show', () => {
    mainWindow.show();
    
    // Initialize NDI Handler
    ndiHandler = new NDIHandler(mainWindow);
    if (ndiHandler.initialize()) {
      console.log('NDI handler initialized successfully');
    }
    
    // Initialize OSC Server with NDI handler
    oscServer = new OSCServer(mainWindow, ndiHandler);
    oscServer.start(8000);
  });

  // ...existing code...
}

// IPC handlers for NDI
ipcMain.handle('ndi-start', async (event, config) => {
  if (ndiHandler) {
    return ndiHandler.start(config);
  }
  return false;
});

ipcMain.handle('ndi-stop', async () => {
  if (ndiHandler) {
    ndiHandler.stop();
    return true;
  }
  return false;
});

ipcMain.handle('ndi-status', async () => {
  if (ndiHandler) {
    return ndiHandler.getStatus();
  }
  return { available: false, active: false };
});

// Add IPC handler for status feedback
ipcMain.on('teleprompter-status', (event, status) => {
    if (oscServer) {
        // Send status back to Companion
        oscServer.sendFeedback('/teleprompter/status', status.isPlaying ? 'playing' : 'stopped');
        oscServer.sendFeedback('/teleprompter/speed/current', status.speed);
        oscServer.sendFeedback('/teleprompter/font/size/current', status.fontSize);
        oscServer.sendFeedback('/teleprompter/position/current', status.position);
        oscServer.sendFeedback('/teleprompter/mirror/status', status.isMirrored ? 'true' : 'false');
    }
});

app.on('before-quit', () => {
  if (oscServer) {
    oscServer.stop();
  }
  if (ndiHandler) {
    ndiHandler.destroy();
  }
});

// ...existing code...
```