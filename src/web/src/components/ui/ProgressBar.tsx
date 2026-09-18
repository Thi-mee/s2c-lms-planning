export interface ProgressBarProps {
  value: number;
  max: number;
  label?: string;
  className?: string;
}

export function ProgressBar({ value, max, label, className = '' }: ProgressBarProps) {
  const percentage = max > 0 ? Math.min(100, Math.max(0, Math.round((value / max) * 100))) : 0;
  const isHigh = percentage >= 90;
  const isMid = percentage >= 75 && !isHigh;

  return (
    <div className={`progress-wrap ${className}`.trim()}>
      {label && (
        <div className="progress-labels">
          <span className="progress-label-text">{label}</span>
          <span className="progress-percentage">{percentage}%</span>
        </div>
      )}
      <div
        className="progress-track"
        role="progressbar"
        aria-valuenow={value}
        aria-valuemin={0}
        aria-valuemax={max}
        aria-label={label ?? `${percentage}% used`}
      >
        <div
          className={`progress-fill ${isHigh ? 'progress-high' : isMid ? 'progress-mid' : ''}`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}
