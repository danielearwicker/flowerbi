import React from "react";

interface LayoutElementCoreProps extends React.HTMLAttributes<HTMLDivElement> {
    sizes?: number[];
    children: React.ReactNode[];
}

interface LayoutElementProps extends LayoutElementCoreProps {
    type: "row" | "column";
    dimension: "width" | "height";
}

function LayoutElement({ children, type, dimension, sizes, ...otherProps }: LayoutElementProps) {
    const nonNullSizes = sizes ?? [];
    const totalSize = children.map((_, i) => nonNullSizes[i] ?? 1).reduce((l, r) => l + r, 0);
    const unit = totalSize ? 100 / totalSize : 0;

    return (
        <div {...otherProps} style={{ display: "flex", flexDirection: type, width: "100%", height: "100%" }}>
            {children.map((child, i) => (
                <div className="layout-item" style={{ [dimension]: `${unit * (nonNullSizes[i] ?? 1)}%` }}>
                    {child}
                </div>
            ))}
        </div>
    );
}

export const Row = (props: LayoutElementCoreProps) => <LayoutElement type="row" dimension="width" {...props} />;
export const Column = (props: LayoutElementCoreProps) => <LayoutElement type="column" dimension="height" {...props} />;

export interface LayoutProps {
    children: React.ReactNode;
}

export function Layout({ children }: LayoutProps) {
    return <div className="layout">{children}</div>;
}
