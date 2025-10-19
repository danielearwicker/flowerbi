import { Children, type HTMLAttributes, type ReactNode } from "react";

interface LayoutElementCoreProps extends HTMLAttributes<HTMLDivElement> {
    sizes?: number[];
    children: ReactNode | ReactNode[];
}

interface LayoutElementProps extends LayoutElementCoreProps {
    type: "row" | "column";
    dimension: "width" | "height";
}

function LayoutElement({
    children,
    type,
    dimension,
    sizes,
    ...otherProps
}: LayoutElementProps) {
    const nonNullSizes = sizes ?? [];
    const totalSize =
        Children.map(children, (_, i) => nonNullSizes[i] ?? 1)?.reduce(
            (l, r) => l + r,
            0
        ) ?? 0;
    const unit = totalSize ? 100 / totalSize : 0;

    return (
        <div
            {...otherProps}
            style={{
                display: "flex",
                flexDirection: type,
                width: "100%",
                height: "100%",
            }}
        >
            {Children.map(children, (child, i) => (
                <div
                    className="layout-item"
                    style={{ [dimension]: `${unit * (nonNullSizes[i] ?? 1)}%` }}
                >
                    {child}
                </div>
            ))}
        </div>
    );
}

export const Row = (props: LayoutElementCoreProps) => (
    <LayoutElement type="row" dimension="width" {...props} />
);
export const Column = (props: LayoutElementCoreProps) => (
    <LayoutElement type="column" dimension="height" {...props} />
);

export interface LayoutProps extends React.HTMLAttributes<HTMLDivElement> {
    children: React.ReactNode | React.ReactNode[];
}

export function Layout({ children, ...otherProps }: LayoutProps) {
    return (
        <div {...otherProps} className="layout">
            {children}
        </div>
    );
}
