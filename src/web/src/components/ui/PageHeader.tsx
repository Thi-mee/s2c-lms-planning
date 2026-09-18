import type { ReactNode, Ref } from 'react';

export interface PageHeaderProps {
  eyebrow?: string;
  title: ReactNode;
  titleId?: string;
  titleRef?: Ref<HTMLHeadingElement>;
  status?: ReactNode;
  description?: ReactNode;
  backLink?: {
    label: string;
    onClick: () => void;
    disabled?: boolean;
  };
  actions?: ReactNode;
  metadata?: ReactNode;
  className?: string;
}

export function PageHeader({
  eyebrow,
  title,
  titleId,
  titleRef,
  status,
  description,
  backLink,
  actions,
  metadata,
  className = '',
}: PageHeaderProps) {
  return (
    <header className={`page-header ${className}`.trim()}>
      {backLink && (
        <div className="page-header-back">
          <button
            type="button"
            className="text-button back-button"
            onClick={backLink.onClick}
            disabled={backLink.disabled}
          >
            {backLink.label}
          </button>
        </div>
      )}

      {eyebrow && <p className="eyebrow">{eyebrow}</p>}

      <div className="page-header-main">
        <div className="page-header-title-row">
          <h1 id={titleId} ref={titleRef} tabIndex={titleRef ? -1 : undefined} className="page-title">
            {title}
          </h1>
          {status && <div className="page-header-status">{status}</div>}
        </div>
        {actions && <div className="page-header-actions actions">{actions}</div>}
      </div>

      {description && <p className="intro page-header-description">{description}</p>}
      {metadata && <div className="page-header-metadata">{metadata}</div>}
    </header>
  );
}
