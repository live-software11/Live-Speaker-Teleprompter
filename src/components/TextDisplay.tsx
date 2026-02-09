import React, { useEffect, useRef, useCallback, useMemo, useState } from 'react';

export type TextDisplayProps = {
  text: string;
  scrollPosition: number;
  fontSize: number;
  lineHeight: number;
  fontColor: string;
  backgroundColor: string;
  scrollSpeed: number;
  isScrolling: boolean;
  highlightCurrentLine: boolean;
  currentLineColor: string;
  mirrorMode: boolean;
  onScrollPositionChange: React.Dispatch<React.SetStateAction<number>>;
};

const ScrollStepIntervalMs = 16; // ~60fps
const OVERSCAN_LINES = 10; // render extra lines above/below viewport

const TextDisplay: React.FC<TextDisplayProps> = ({
  text,
  scrollPosition,
  fontSize,
  lineHeight,
  fontColor,
  backgroundColor,
  scrollSpeed,
  isScrolling,
  highlightCurrentLine,
  currentLineColor,
  mirrorMode,
  onScrollPositionChange
}) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const animationFrameRef = useRef<number | null>(null);
  const lastFrameTimestampRef = useRef<number>(performance.now());
  const [visibleRange, setVisibleRange] = useState({ start: 0, end: 100 });

  const cancelAnimation = useCallback(() => {
    if (animationFrameRef.current !== null) {
      cancelAnimationFrame(animationFrameRef.current);
      animationFrameRef.current = null;
    }
  }, []);

  const step = useCallback(() => {
    const container = containerRef.current;
    if (!container) {
      cancelAnimation();
      return;
    }

    const now = performance.now();
    const deltaMs = now - lastFrameTimestampRef.current;

    if (deltaMs >= ScrollStepIntervalMs) {
      lastFrameTimestampRef.current = now;
      const deltaPixels = (scrollSpeed * deltaMs) / 1000;
      const nextOffset = container.scrollTop + deltaPixels;
      const maxScroll = container.scrollHeight - container.clientHeight;

      if (nextOffset >= maxScroll) {
        container.scrollTop = 0;
        onScrollPositionChange(0);
      } else {
        container.scrollTop = nextOffset;
        onScrollPositionChange(nextOffset);
      }
    }

    animationFrameRef.current = requestAnimationFrame(step);
  }, [cancelAnimation, scrollSpeed, onScrollPositionChange]);

  useEffect(() => {
    if (!isScrolling) {
      cancelAnimation();
      return undefined;
    }

    lastFrameTimestampRef.current = performance.now();
    animationFrameRef.current = requestAnimationFrame(step);

    return cancelAnimation;
  }, [cancelAnimation, isScrolling, step]);

  // Only apply external scrollPosition changes when NOT auto-scrolling.
  // During auto-scroll, scrollTop is set directly in the step() function,
  // and calling scrollTo({ smooth }) every frame would conflict.
  useEffect(() => {
    if (isScrolling) {
      return;
    }

    const container = containerRef.current;
    if (!container) {
      return;
    }

    container.scrollTo({ top: scrollPosition, behavior: 'smooth' });
  }, [scrollPosition, isScrolling]);

  const lines = useMemo(() => text.split(/\r?\n/), [text]);
  const effectiveLineHeight = useMemo(() => (lineHeight <= 0 ? 1 : lineHeight), [lineHeight]);
  const lineHeightPx = useMemo(() => fontSize * effectiveLineHeight, [fontSize, effectiveLineHeight]);

  // Compute which lines are visible and update range
  const updateVisibleRange = useCallback(() => {
    const container = containerRef.current;
    if (!container || lineHeightPx <= 0) return;

    const scrollTop = container.scrollTop;
    const viewportHeight = container.clientHeight;

    const firstVisible = Math.floor(scrollTop / lineHeightPx);
    const lastVisible = Math.ceil((scrollTop + viewportHeight) / lineHeightPx);

    const start = Math.max(0, firstVisible - OVERSCAN_LINES);
    const end = Math.min(lines.length, lastVisible + OVERSCAN_LINES);

    setVisibleRange(prev => {
      if (prev.start === start && prev.end === end) return prev;
      return { start, end };
    });
  }, [lineHeightPx, lines.length]);

  // Update visible range on scroll
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const onScroll = () => updateVisibleRange();
    container.addEventListener('scroll', onScroll, { passive: true });
    updateVisibleRange();

    return () => container.removeEventListener('scroll', onScroll);
  }, [updateVisibleRange]);

  // Recalculate on resize
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const observer = new ResizeObserver(() => updateVisibleRange());
    observer.observe(container);
    return () => observer.disconnect();
  }, [updateVisibleRange]);

  const currentLineIndex = useMemo(() => {
    if (!highlightCurrentLine || lineHeightPx <= 0) {
      return -1;
    }
    return Math.round(scrollPosition / lineHeightPx);
  }, [highlightCurrentLine, lineHeightPx, scrollPosition]);

  const handleWheel = useCallback(
    (event: React.WheelEvent<HTMLDivElement>) => {
      const container = containerRef.current;
      if (!container) {
        return;
      }

      event.preventDefault();
      cancelAnimation();

      const nextOffset = container.scrollTop + event.deltaY;
      container.scrollTop = nextOffset;
      onScrollPositionChange(nextOffset);
    },
    [cancelAnimation, onScrollPositionChange]
  );

  // Virtualized rendering: only render visible lines + overscan
  const totalHeight = lines.length * lineHeightPx;
  const offsetTop = visibleRange.start * lineHeightPx;
  const visibleLines = lines.slice(visibleRange.start, visibleRange.end);

  return (
    <div
      ref={containerRef}
      onWheel={handleWheel}
      className={`text-display${mirrorMode ? ' text-display--mirrored' : ''}`}
      style={{
        backgroundColor,
        color: fontColor,
        fontSize: `${fontSize}px`,
        lineHeight: effectiveLineHeight.toString()
      }}
    >
      <div
        className="text-display__content"
        style={{ height: `${totalHeight}px`, position: 'relative' }}
      >
        <div style={{ position: 'absolute', top: `${offsetTop}px`, width: '100%' }}>
          {visibleLines.map((line, i) => {
            const absoluteIndex = visibleRange.start + i;
            const isCurrent = absoluteIndex === currentLineIndex;
            return (
              <div
                key={absoluteIndex}
                className="text-line"
                style={{
                  height: `${lineHeightPx}px`,
                  background: isCurrent ? currentLineColor : 'transparent'
                }}
              >
                {line || '\u00A0'}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default TextDisplay;