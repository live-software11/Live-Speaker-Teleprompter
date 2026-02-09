const osc = require('osc');

class OSCServer {
  constructor(mainWindow, ndiHandler) {
    this.mainWindow = mainWindow;
    this.ndiHandler = ndiHandler;
    this.udpPort = null;
    this.isRunning = false;
  }

  start(port = 8000) {
    if (this.isRunning) {
      console.log('OSC Server already running');
      return;
    }

    try {
      this.udpPort = new osc.UDPPort({
        localAddress: '0.0.0.0',
        localPort: port,
        metadata: true
      });

      this.udpPort.on('ready', () => {
        console.log(`OSC Server listening on port ${port}`);
        this.isRunning = true;
        this.mainWindow.webContents.send('osc-status', { connected: true, port });
      });

      this.udpPort.on('message', (oscMessage) => {
        console.log('OSC Message received:', oscMessage);
        this.handleOSCMessage(oscMessage);
      });

      this.udpPort.on('error', (error) => {
        console.error('OSC Error:', error);
        this.mainWindow.webContents.send('osc-error', error.message);
      });

      this.udpPort.open();
    } catch (error) {
      console.error('Failed to start OSC server:', error);
      this.mainWindow.webContents.send('osc-error', error.message);
    }
  }

  handleOSCMessage(oscMessage) {
    const { address, args } = oscMessage;
    
    // Log per debug
    console.log(`OSC Command: ${address}, Args:`, args);
    
    switch(address) {
      // Controllo riproduzione
      case '/teleprompter/start':
      case '/teleprompter/play':
        this.mainWindow.webContents.send('teleprompter-control', 'start');
        this.sendFeedback('/teleprompter/status', 'playing');
        break;
      
      case '/teleprompter/stop':
      case '/teleprompter/pause':
        this.mainWindow.webContents.send('teleprompter-control', 'stop');
        this.sendFeedback('/teleprompter/status', 'stopped');
        break;
      
      case '/teleprompter/reset':
        this.mainWindow.webContents.send('teleprompter-control', 'reset');
        this.sendFeedback('/teleprompter/status', 'reset');
        break;
      
      // Controllo velocità
      case '/teleprompter/speed':
        if (args && args[0]) {
          const speed = args[0].value;
          this.mainWindow.webContents.send('teleprompter-speed', speed);
          this.sendFeedback('/teleprompter/speed/current', speed);
        }
        break;
      
      case '/teleprompter/speed/increase':
        this.mainWindow.webContents.send('teleprompter-speed-adjust', 'increase');
        break;
      
      case '/teleprompter/speed/decrease':
        this.mainWindow.webContents.send('teleprompter-speed-adjust', 'decrease');
        break;
      
      // Controllo font
      case '/teleprompter/font/size':
        if (args && args[0]) {
          const size = args[0].value;
          this.mainWindow.webContents.send('teleprompter-font-size', size);
          this.sendFeedback('/teleprompter/font/size/current', size);
        }
        break;
      
      case '/teleprompter/font/increase':
        this.mainWindow.webContents.send('teleprompter-font-adjust', 'increase');
        break;
      
      case '/teleprompter/font/decrease':
        this.mainWindow.webContents.send('teleprompter-font-adjust', 'decrease');
        break;
      
      // Navigazione script
      case '/teleprompter/script/next':
        this.mainWindow.webContents.send('teleprompter-script', 'next');
        break;
      
      case '/teleprompter/script/previous':
        this.mainWindow.webContents.send('teleprompter-script', 'previous');
        break;
      
      case '/teleprompter/script/load':
        if (args && args[0]) {
          const scriptIndex = args[0].value;
          this.mainWindow.webContents.send('teleprompter-script-load', scriptIndex);
        }
        break;
      
      // Controllo posizione
      case '/teleprompter/position':
        if (args && args[0]) {
          const position = args[0].value;
          this.mainWindow.webContents.send('teleprompter-position', position);
        }
        break;
      
      case '/teleprompter/jump/top':
        this.mainWindow.webContents.send('teleprompter-jump', 'top');
        break;
      
      case '/teleprompter/jump/bottom':
        this.mainWindow.webContents.send('teleprompter-jump', 'bottom');
        break;
      
      // Mirror mode
      case '/teleprompter/mirror':
        if (args && args[0]) {
          const enabled = args[0].value > 0;
          this.mainWindow.webContents.send('teleprompter-mirror', enabled);
        }
        break;
      
      case '/teleprompter/mirror/toggle':
        this.mainWindow.webContents.send('teleprompter-mirror-toggle');
        break;
      
      // Richiesta stato
      case '/teleprompter/status/request':
        this.mainWindow.webContents.send('teleprompter-status-request');
        break;
      
      // NDI Controls
      case '/ndi/start':
        if (this.ndiHandler) {
          const success = this.ndiHandler.start();
          this.sendFeedback('/ndi/status', success ? 'active' : 'failed');
        }
        break;
      
      case '/ndi/stop':
        if (this.ndiHandler) {
          this.ndiHandler.stop();
          this.sendFeedback('/ndi/status', 'stopped');
        }
        break;
      
      case '/ndi/toggle':
        if (this.ndiHandler) {
          const status = this.ndiHandler.getStatus();
          if (status.active) {
            this.ndiHandler.stop();
          } else {
            this.ndiHandler.start();
          }
        }
        break;
      
      case '/ndi/resolution':
        if (this.ndiHandler && args && args[0] && args[1]) {
          const width = args[0].value;
          const height = args[1].value;
          this.ndiHandler.setResolution(width, height);
          this.sendFeedback('/ndi/resolution/current', `${width}x${height}`);
        }
        break;
      
      case '/ndi/framerate':
        if (this.ndiHandler && args && args[0]) {
          const fps = args[0].value;
          this.ndiHandler.setFrameRate(fps);
          this.sendFeedback('/ndi/framerate/current', fps);
        }
        break;
      
      case '/ndi/sourcename':
        if (this.ndiHandler && args && args[0]) {
          const name = args[0].value;
          this.ndiHandler.setSourceName(name);
          this.sendFeedback('/ndi/sourcename/current', name);
        }
        break;
      
      case '/ndi/status/request':
        if (this.ndiHandler) {
          const status = this.ndiHandler.getStatus();
          this.sendFeedback('/ndi/status', status.active ? 'active' : 'inactive');
          this.sendFeedback('/ndi/available', status.available ? 'yes' : 'no');
        }
        break;
      
      // Output mode selection
      case '/output/ndi':
        this.mainWindow.webContents.send('output-mode', 'ndi');
        if (this.ndiHandler) {
          this.ndiHandler.start();
        }
        break;
      
      case '/output/display':
        this.mainWindow.webContents.send('output-mode', 'display');
        if (this.ndiHandler) {
          this.ndiHandler.stop();
        }
        break;
      
      case '/output/both':
        this.mainWindow.webContents.send('output-mode', 'both');
        if (this.ndiHandler) {
          this.ndiHandler.start();
        }
        break;

      default:
        console.log(`Unknown OSC command: ${address}`);
        break;
    }
  }

  sendFeedback(address, value) {
    if (this.udpPort && this.isRunning) {
      try {
        this.udpPort.send({
          address: address,
          args: [{ type: 's', value: value.toString() }]
        }, '127.0.0.1', 8001); // Porta di feedback per Companion
      } catch (error) {
        console.error('Error sending OSC feedback:', error);
      }
    }
  }

  stop() {
    if (this.udpPort) {
      this.udpPort.close();
      this.isRunning = false;
      console.log('OSC Server stopped');
    }
  }
}

module.exports = OSCServer;
