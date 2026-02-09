import React, { useCallback, useRef, useEffect, useState } from 'react';

interface ControlPanelProps {
  fontSize: number;
  lineHeight: number;
  scrollSpeed: number;
  onFontSizeChange: (size: number) => void;
  onLineHeightChange: (height: number) => void;
  onScrollSpeedChange: (speed: number) => void;
}

/**
 * Custom hook that creates an independent debounced callback.
 * Each call gets its own timer — no cross-handler cancellation.
 */
function useDebouncedCallback<T>(
  callback: (value: T) => void,
  delay: number = 100
) {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const callbackRef = useRef(callback);
  callbackRef.current = callback;

  useEffect(() => {
    return () => {
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current);
      }
    };
  }, []);

  return useCallback(
    (value: T) => {
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current);
      }
      timerRef.current = setTimeout(() => {
        callbackRef.current(value);
      }, delay);
    },
    [delay]
  );
}

/**
 * Hook for a slider that needs immediate visual feedback with a debounced
 * callback to the parent. Returns [localValue, onChange] where localValue
 * tracks the slider position in real time and onChange fires the debounced
 * parent callback.
 */
function useLocalSlider(
  propValue: number,
  onCommit: (value: number) => void,
  delay: number = 100
): [number, (value: number) => void] {
  const [local, setLocal] = useState(propValue);
  const debouncedCommit = useDebouncedCallback(onCommit, delay);

  // Sync from parent when prop changes externally (not from our own drag)
  useEffect(() => {
    setLocal(propValue);
  }, [propValue]);

  const handleChange = useCallback(
    (value: number) => {
      setLocal(value);          // immediate visual feedback
      debouncedCommit(value);   // debounced parent update
    },
    [debouncedCommit]
  );

  return [local, handleChange];
}

const ControlPanel: React.FC<ControlPanelProps> = ({
  fontSize,
  lineHeight,
  scrollSpeed,
  onFontSizeChange,
  onLineHeightChange,
  onScrollSpeedChange,
}) => {
  const [localFontSize, setLocalFontSize] = useLocalSlider(fontSize, onFontSizeChange);
  const [localLineHeight, setLocalLineHeight] = useLocalSlider(lineHeight, onLineHeightChange);
  const [localScrollSpeed, setLocalScrollSpeed] = useLocalSlider(scrollSpeed, onScrollSpeedChange);

  return (
    <div className="control-panel">
      <div className="control-group">
        <label htmlFor="fontSize">Font Size: {localFontSize}px</label>
        <input
          type="range"
          id="fontSize"
          min="12"
          max="72"
          value={localFontSize}
          onChange={(e) => setLocalFontSize(Number(e.target.value))}
        />
      </div>
      <div className="control-group">
        <label htmlFor="lineHeight">Line Height: {localLineHeight}</label>
        <input
          type="range"
          id="lineHeight"
          min="1"
          max="3"
          step="0.1"
          value={localLineHeight}
          onChange={(e) => setLocalLineHeight(Number(e.target.value))}
        />
      </div>
      <div className="control-group">
        <label htmlFor="scrollSpeed">Scroll Speed: {localScrollSpeed}px/s</label>
        <input
          type="range"
          id="scrollSpeed"
          min="10"
          max="200"
          value={localScrollSpeed}
          onChange={(e) => setLocalScrollSpeed(Number(e.target.value))}
        />
      </div>
    </div>
  );
};

export default React.memo(ControlPanel);