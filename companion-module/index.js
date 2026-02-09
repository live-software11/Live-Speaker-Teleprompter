const { InstanceBase, InstanceStatus, runEntrypoint } = require('@companion-module/base');
const osc = require('osc');

class RSpeakerTeleprompterInstance extends InstanceBase {
  constructor(internal) {
    super(internal);
    this.oscPort = null;
    this.feedbackValues = {
      isPlaying: false,
      currentSpeed: 5,
      isMirrored: false
    };
  }

  async init(config) {
    this.config = config;
    this.updateStatus(InstanceStatus.Connecting);
    
    await this.configureOsc();
    this.initActions();
    this.initFeedbacks();
    this.initPresets();
    this.subscribeFeedbacks();
  }

  async configureOsc() {
    if (this.oscPort) {
      this.oscPort.close();
    }

    try {
      this.oscPort = new osc.UDPPort({
        localAddress: '0.0.0.0',
        localPort: this.config.feedbackPort || 8001,
        remoteAddress: this.config.host || '127.0.0.1',
        remotePort: this.config.port || 8000,
        metadata: true
      });

      this.oscPort.on('ready', () => {
        this.updateStatus(InstanceStatus.Ok);
        this.log('info', `Connected to R-Speaker Teleprompter at ${this.config.host}:${this.config.port}`);
      });

      this.oscPort.on('message', (oscMsg) => {
        this.processFeedback(oscMsg);
      });

      this.oscPort.on('error', (err) => {
        this.updateStatus(InstanceStatus.ConnectionFailure);
        this.log('error', `OSC error: ${err.message}`);
      });

      this.oscPort.open();
    } catch (err) {
      this.updateStatus(InstanceStatus.ConnectionFailure);
      this.log('error', `Failed to setup OSC: ${err.message}`);
    }
  }

  processFeedback(oscMsg) {
    const { address, args } = oscMsg;
    
    switch(address) {
      case '/teleprompter/status':
        if (args[0]) {
          this.feedbackValues.isPlaying = args[0].value === 'playing';
          this.checkFeedbacks('isPlaying');
        }
        break;
      
      case '/teleprompter/speed/current':
        if (args[0]) {
          this.feedbackValues.currentSpeed = args[0].value;
          this.checkFeedbacks('currentSpeed');
        }
        break;
      
      case '/teleprompter/mirror/status':
        if (args[0]) {
          this.feedbackValues.isMirrored = args[0].value === 'true';
          this.checkFeedbacks('isMirrored');
        }
        break;
        
      case '/ndi/status':
        if (args[0]) {
          this.feedbackValues.ndiActive = args[0].value === 'active';
          this.checkFeedbacks('ndiActive');
        }
        break;
      
      case '/ndi/available':
        if (args[0]) {
          this.feedbackValues.ndiAvailable = args[0].value === 'yes';
          this.checkFeedbacks('ndiAvailable');
        }
        break;
    }
  }

  sendOsc(address, args = []) {
    if (this.oscPort) {
      try {
        this.oscPort.send({ address, args });
        this.log('debug', `Sent OSC: ${address}`);
      } catch (err) {
        this.log('error', `Failed to send OSC: ${err.message}`);
      }
    }
  }

  initActions() {
    this.setActionDefinitions({
      play: {
        name: 'Start/Play',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/start');
        }
      },
      stop: {
        name: 'Stop/Pause',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/stop');
        }
      },
      reset: {
        name: 'Reset',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/reset');
        }
      },
      setSpeed: {
        name: 'Set Speed',
        options: [
          {
            type: 'number',
            label: 'Speed (1-10)',
            id: 'speed',
            default: 5,
            min: 1,
            max: 10
          }
        ],
        callback: async (action) => {
          this.sendOsc('/teleprompter/speed', [
            { type: 'i', value: action.options.speed }
          ]);
        }
      },
      speedUp: {
        name: 'Speed Increase',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/speed/increase');
        }
      },
      speedDown: {
        name: 'Speed Decrease',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/speed/decrease');
        }
      },
      setFontSize: {
        name: 'Set Font Size',
        options: [
          {
            type: 'number',
            label: 'Font Size',
            id: 'size',
            default: 48,
            min: 20,
            max: 200
          }
        ],
        callback: async (action) => {
          this.sendOsc('/teleprompter/font/size', [
            { type: 'i', value: action.options.size }
          ]);
        }
      },
      fontUp: {
        name: 'Font Increase',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/font/increase');
        }
      },
      fontDown: {
        name: 'Font Decrease',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/font/decrease');
        }
      },
      nextScript: {
        name: 'Next Script',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/script/next');
        }
      },
      prevScript: {
        name: 'Previous Script',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/script/previous');
        }
      },
      loadScript: {
        name: 'Load Script',
        options: [
          {
            type: 'number',
            label: 'Script Index',
            id: 'index',
            default: 0,
            min: 0
          }
        ],
        callback: async (action) => {
          this.sendOsc('/teleprompter/script/load', [
            { type: 'i', value: action.options.index }
          ]);
        }
      },
      mirrorToggle: {
        name: 'Mirror Toggle',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/mirror/toggle');
        }
      },
      jumpTop: {
        name: 'Jump to Top',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/jump/top');
        }
      },
      jumpBottom: {
        name: 'Jump to Bottom',
        options: [],
        callback: async () => {
          this.sendOsc('/teleprompter/jump/bottom');
        }
      },
      setPosition: {
        name: 'Set Position',
        options: [
          {
            type: 'number',
            label: 'Position (%)',
            id: 'position',
            default: 0,
            min: 0,
            max: 100
          }
        ],
        callback: async (action) => {
          this.sendOsc('/teleprompter/position', [
            { type: 'f', value: action.options.position / 100 }
          ]);
        }
      },
      // NDI Actions
      ndiStart: {
        name: 'NDI Start',
        options: [],
        callback: async () => {
          this.sendOsc('/ndi/start');
        }
      },
      ndiStop: {
        name: 'NDI Stop',
        options: [],
        callback: async () => {
          this.sendOsc('/ndi/stop');
        }
      },
      ndiToggle: {
        name: 'NDI Toggle',
        options: [],
        callback: async () => {
          this.sendOsc('/ndi/toggle');
        }
      },
      ndiResolution: {
        name: 'NDI Set Resolution',
        options: [
          {
            type: 'dropdown',
            label: 'Resolution',
            id: 'resolution',
            default: '1920x1080',
            choices: [
              { id: '1920x1080', label: 'Full HD (1920x1080)' },
              { id: '1280x720', label: 'HD (1280x720)' },
              { id: '3840x2160', label: '4K (3840x2160)' }
            ]
          }
        ],
        callback: async (action) => {
          const [width, height] = action.options.resolution.split('x').map(Number);
          this.sendOsc('/ndi/resolution', [
            { type: 'i', value: width },
            { type: 'i', value: height }
          ]);
        }
      },
      ndiFramerate: {
        name: 'NDI Set Framerate',
        options: [
          {
            type: 'dropdown',
            label: 'Framerate',
            id: 'fps',
            default: '30',
            choices: [
              { id: '25', label: '25 fps' },
              { id: '30', label: '30 fps' },
              { id: '50', label: '50 fps' },
              { id: '60', label: '60 fps' }
            ]
          }
        ],
        callback: async (action) => {
          this.sendOsc('/ndi/framerate', [
            { type: 'i', value: parseInt(action.options.fps) }
          ]);
        }
      },
      outputMode: {
        name: 'Set Output Mode',
        options: [
          {
            type: 'dropdown',
            label: 'Output Mode',
            id: 'mode',
            default: 'display',
            choices: [
              { id: 'display', label: 'Display Only' },
              { id: 'ndi', label: 'NDI Only' },
              { id: 'both', label: 'Both Display + NDI' }
            ]
          }
        ],
        callback: async (action) => {
          this.sendOsc(`/output/${action.options.mode}`);
        }
      }
    });
  }

  initFeedbacks() {
    this.setFeedbackDefinitions({
      isPlaying: {
        type: 'boolean',
        name: 'Playing Status',
        defaultStyle: {
          bgcolor: this.rgb(0, 255, 0),
          color: this.rgb(255, 255, 255)
        },
        options: [],
        callback: () => {
          return this.feedbackValues.isPlaying;
        }
      },
      currentSpeed: {
        type: 'advanced',
        name: 'Current Speed',
        options: [],
        callback: () => {
          return {
            text: `Speed: ${this.feedbackValues.currentSpeed}`
          };
        }
      },
      isMirrored: {
        type: 'boolean',
        name: 'Mirror Status',
        defaultStyle: {
          bgcolor: this.rgb(0, 0, 255),
          color: this.rgb(255, 255, 255)
        },
        options: [],
        callback: () => {
          return this.feedbackValues.isMirrored;
        }
      },
      ndiActive: {
        type: 'boolean',
        name: 'NDI Active',
        defaultStyle: {
          bgcolor: this.rgb(255, 0, 0),
          color: this.rgb(255, 255, 255)
        },
        options: [],
        callback: () => {
          return this.feedbackValues.ndiActive || false;
        }
      },
      ndiAvailable: {
        type: 'boolean',
        name: 'NDI Available',
        defaultStyle: {
          bgcolor: this.rgb(0, 255, 0),
          color: this.rgb(255, 255, 255)
        },
        options: [],
        callback: () => {
          return this.feedbackValues.ndiAvailable || false;
        }
      }
    });
  }

  initPresets() {
    const presets = [];
    
    // Playback presets
    presets.push({
      type: 'button',
      category: 'Playback',
      name: 'Play',
      style: {
        text: 'PLAY',
        size: '18',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(0, 255, 0)
      },
      steps: [{ down: [{ actionId: 'play' }] }],
      feedbacks: [{ feedbackId: 'isPlaying' }]
    });
    
    presets.push({
      type: 'button',
      category: 'Playback',
      name: 'Stop',
      style: {
        text: 'STOP',
        size: '18',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(255, 0, 0)
      },
      steps: [{ down: [{ actionId: 'stop' }] }]
    });
    
    presets.push({
      type: 'button',
      category: 'Playback',
      name: 'Reset',
      style: {
        text: 'RESET',
        size: '18',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(0, 0, 255)
      },
      steps: [{ down: [{ actionId: 'reset' }] }]
    });
    
    // Speed presets
    presets.push({
      type: 'button',
      category: 'Speed',
      name: 'Speed Up',
      style: {
        text: 'SPEED+',
        size: '14',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(255, 165, 0)
      },
      steps: [{ down: [{ actionId: 'speedUp' }] }]
    });
    
    presets.push({
      type: 'button',
      category: 'Speed',
      name: 'Speed Down',
      style: {
        text: 'SPEED-',
        size: '14',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(255, 165, 0)
      },
      steps: [{ down: [{ actionId: 'speedDown' }] }]
    });
    
    // NDI presets
    presets.push({
      type: 'button',
      category: 'NDI Output',
      name: 'NDI Toggle',
      style: {
        text: 'NDI',
        size: '18',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(255, 0, 0)
      },
      steps: [{ down: [{ actionId: 'ndiToggle' }] }],
      feedbacks: [
        { 
          feedbackId: 'ndiActive',
          style: { bgcolor: this.rgb(0, 255, 0) }
        }
      ]
    });
    
    presets.push({
      type: 'button',
      category: 'NDI Output',
      name: 'Output: Display',
      style: {
        text: 'DISPLAY',
        size: '14',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(100, 100, 100)
      },
      steps: [{ 
        down: [{ 
          actionId: 'outputMode',
          options: { mode: 'display' }
        }] 
      }]
    });
    
    presets.push({
      type: 'button',
      category: 'NDI Output',
      name: 'Output: NDI',
      style: {
        text: 'NDI ONLY',
        size: '14',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(255, 0, 0)
      },
      steps: [{ 
        down: [{ 
          actionId: 'outputMode',
          options: { mode: 'ndi' }
        }] 
      }]
    });
    
    presets.push({
      type: 'button',
      category: 'NDI Output',
      name: 'Output: Both',
      style: {
        text: 'BOTH',
        size: '14',
        color: this.rgb(255, 255, 255),
        bgcolor: this.rgb(0, 150, 0)
      },
      steps: [{ 
        down: [{ 
          actionId: 'outputMode',
          options: { mode: 'both' }
        }] 
      }]
    });
    
    this.setPresetDefinitions(presets);
  }

  async destroy() {
    if (this.oscPort) {
      this.oscPort.close();
      this.oscPort = null;
    }
  }

  async configUpdated(config) {
    this.config = config;
    await this.configureOsc();
  }

  getConfigFields() {
    return [
      {
        type: 'textinput',
        id: 'host',
        label: 'Target IP',
        width: 8,
        default: '127.0.0.1'
      },
      {
        type: 'number',
        id: 'port',
        label: 'OSC Port',
        width: 4,
        default: 8000,
        min: 1,
        max: 65535
      },
      {
        type: 'number',
        id: 'feedbackPort',
        label: 'Feedback Port',
        width: 4,
        default: 8001,
        min: 1,
        max: 65535
      }
    ];
  }
}

runEntrypoint(RSpeakerTeleprompterInstance, []);
