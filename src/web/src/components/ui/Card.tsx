import type { HTMLAttributes, ReactNode } from 'react';

export interface CardProps extends HTMLAttributes<HTMLElement> {
  as?: 'section' | 'article' | 'div';
  children: ReactNode;
  header?: ReactNode;
  footer?: ReactNode;
  variant?: 'default' | 'raised' | 'muted' | 'dashed';
}

export function Card({
  as: Component = 'section',
  children,
  header,
  footer,
  variant = 'default',
  className = '',
  ...props
}: CardProps) {
  return (
    <Component
      className={`card-base card-${variant} ${className}`.trim()}
      {...props}
    >
      {header && <div className="card-header">{header}</div>}
      <div className="card-body">{children}</div>
      {footer && <div className="card-footer">{footer}</div>}
    </Component>
  );
}
