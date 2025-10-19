import { FlowerBIChartBox } from "./FlowerBIChartBox";

export interface FlowerBIValueBoxProps {
    id?: string;
    value?: string | number;
    title?: string;
    label?: string;
}

export function FlowerBIValueBox({
    id,
    value,
    title,
    label,
}: FlowerBIValueBoxProps) {
    return (
        <FlowerBIChartBox id={id} title={title}>
            <div className="value-box">
                <div className="value">{value}</div>
                <div className="title">{label}</div>
            </div>
        </FlowerBIChartBox>
    );
}
