const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
const SUPPORTED_TYPES = ['text/plain', 'text/markdown', 'text/html', 'application/rtf'];

export const readFileAsText = (file: File): Promise<string> => {
  return new Promise((resolve, reject) => {
    // Validation
    if (!file) {
      reject(new Error('No file provided'));
      return;
    }

    if (file.size > MAX_FILE_SIZE) {
      reject(new Error(`File size exceeds ${MAX_FILE_SIZE / 1024 / 1024}MB limit`));
      return;
    }

    if (!SUPPORTED_TYPES.some(type => file.type.startsWith(type)) && 
        !file.name.match(/\.(txt|md|rtf|html)$/i)) {
      reject(new Error('Unsupported file type'));
      return;
    }

    const reader = new FileReader();
    
    const timeout = setTimeout(() => {
      reader.abort();
      reject(new Error('File reading timeout'));
    }, 30000); // 30 second timeout

    reader.onload = (e) => {
      clearTimeout(timeout);
      const result = e.target?.result as string;
      if (result) {
        resolve(result);
      } else {
        reject(new Error('Failed to read file content'));
      }
    };

    reader.onerror = () => {
      clearTimeout(timeout);
      reject(new Error('Error reading file: ' + reader.error?.message));
    };

    reader.onabort = () => {
      clearTimeout(timeout);
      reject(new Error('File reading was aborted'));
    };

    try {
      reader.readAsText(file);
    } catch (error) {
      clearTimeout(timeout);
      reject(error);
    }
  });
};

export const saveTextToFile = async (text: string, fileName: string): Promise<void> => {
  try {
    const blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    
    // Cleanup
    setTimeout(() => {
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    }, 100);
  } catch (error) {
    console.error('Error saving file:', error);
    throw new Error('Failed to save file');
  }
};
