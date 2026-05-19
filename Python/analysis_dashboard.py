from __future__ import annotations

from datetime import datetime, timedelta
import random

import dash
from dash import Dash, Input, Output, dcc, html, dash_table
import numpy as np
import pandas as pd
import plotly.graph_objects as go
import pyodbc


REFRESH_MS = 5_000
HISTORY_HOURS = 24
MOVING_AVG_WINDOW = 10

SQL_CONNECTION_STRING = (
    "Server=LHsPC\\SQLEXPRESS;"
    "Database=LIBRARY;"
    "Trusted_Connection=yes;"
    "TrustServerCertificate=yes;"
    "Driver={ODBC Driver 17 for SQL Server};"
)


def get_sql_connection():
    return pyodbc.connect(SQL_CONNECTION_STRING)


def fetch_realtime_data() -> pd.DataFrame:
    query = """
    SELECT
        COALESCE(cm.ServerTimestamp, cm.SourceTimestamp, cm.LastUpdated) AS timestamp,
        COALESCE(od.DisplayName, CONCAT('OpcDatapoint ', CAST(cm.OpcDatapointId AS VARCHAR(20)))) AS sensor,
        cm.MeasurementValue AS value
    FROM dbo.CurrentMeasurement cm
    LEFT JOIN dbo.OpcDatapoints od ON cm.OpcDatapointId = od.OpcDatapointId
    WHERE COALESCE(od.IsActive, 1) = 1
    ORDER BY od.DisplayName, cm.ServerTimestamp DESC
    """
    try:
        conn = get_sql_connection()
        df = pd.read_sql(query, conn)
        conn.close()
        df["timestamp"] = pd.to_datetime(df["timestamp"]).dt.floor("s")
        df["value"] = pd.to_numeric(df["value"], errors="coerce").round(2)
        return df
    except Exception as e:
        print(f"Error fetching real-time data: {e}")
        return pd.DataFrame()


def fetch_historical_data(hours: int = HISTORY_HOURS) -> pd.DataFrame:
    query = f"""
    SELECT
        COALESCE(m.SourceTimestamp, m.MeasurementTimeStamp, m.ServerTimestamp) AS timestamp,
        COALESCE(od.DisplayName, CONCAT('OpcDatapoint ', CAST(m.OpcDatapointId AS VARCHAR(20)))) AS sensor,
        m.MeasurementValue AS value
    FROM dbo.Measurement m
    LEFT JOIN dbo.OpcDatapoints od ON m.OpcDatapointId = od.OpcDatapointId
    WHERE COALESCE(od.IsActive, 1) = 1
        AND COALESCE(m.SourceTimestamp, m.MeasurementTimeStamp, m.ServerTimestamp) >= DATEADD(HOUR, -{hours}, GETUTCDATE())
    ORDER BY COALESCE(m.SourceTimestamp, m.MeasurementTimeStamp, m.ServerTimestamp) DESC
    """
    try:
        conn = get_sql_connection()
        df = pd.read_sql(query, conn)
        conn.close()
        df["timestamp"] = pd.to_datetime(df["timestamp"]).dt.floor("s")
        df["value"] = pd.to_numeric(df["value"], errors="coerce").round(2)
        return df
    except Exception as e:
        print(f"Error fetching historical data: {e}")
        return pd.DataFrame()


def fetch_from_sql_server(use_historical: bool = True) -> pd.DataFrame:
    if use_historical:
        df = fetch_historical_data(HISTORY_HOURS)
        if not df.empty:
            return df

        realtime_df = fetch_realtime_data()
        if not realtime_df.empty:
            return realtime_df

        return pd.DataFrame()

    df = fetch_realtime_data()
    if not df.empty:
        return df

    return fetch_historical_data(HISTORY_HOURS)


def get_data(use_sql_server: bool = True) -> pd.DataFrame:
    if not use_sql_server:
        return pd.DataFrame()

    try:
        return fetch_from_sql_server(use_historical=True)
    except Exception as e:
        print(f"Failed to fetch from SQL Server: {e}")
        return pd.DataFrame()


def latest_snapshot(df: pd.DataFrame) -> pd.DataFrame:
    latest_idx = df.groupby("sensor")["timestamp"].idxmax()
    return df.loc[latest_idx].sort_values("sensor").reset_index(drop=True)


def make_kpi_cards(snapshot: pd.DataFrame) -> list:
    cards = []
    for _, row in snapshot.iterrows():
        timestamp_text = row["timestamp"].strftime("%Y-%m-%d %H:%M:%S")
        cards.append(
            html.Div(
                [
                    html.Div(row["sensor"], className="card-title"),
                    html.Div(f"{row['value']:.2f}", className="card-value"),
                    html.Div(f"Last Updated: {timestamp_text}", className="card-sub"),
                ],
                className="sensor-card",
            )
        )
    return cards


def build_live_data_table(snapshot: pd.DataFrame) -> pd.DataFrame:
    live_df = snapshot.copy()
    live_df["value"] = pd.to_numeric(live_df["value"], errors="coerce").round(2)
    live_df["timestamp"] = pd.to_datetime(live_df["timestamp"]).dt.strftime("%Y-%m-%d %H:%M:%S")
    return live_df[["sensor", "value", "timestamp"]].rename(
        columns={
            "sensor": "Process Data",
            "value": "Value [°C]",
            "timestamp": "Last Updated",
        }
    )


def control_system_figure(df: pd.DataFrame, ma_window: int = MOVING_AVG_WINDOW) -> go.Figure:
    fig = go.Figure()

    if df.empty:
        fig.add_annotation(text="No data available")
        return fig

    plot_df = df[~df["sensor"].astype(str).str.contains("setpoint", case=False, na=False)].copy()
    if plot_df.empty:
        fig.add_annotation(text="No measured sensor data available")
        return fig

    for sensor in plot_df["sensor"].unique():
        sdf = plot_df[plot_df["sensor"] == sensor].sort_values("timestamp").copy()
        sdf["value_ma"] = sdf["value"].rolling(window=ma_window, min_periods=1).mean().round(2)

        fig.add_trace(
            go.Scatter(
                x=sdf["timestamp"],
                y=sdf["value"],
                mode="lines",
                name=f"{sensor} Process Value [°C]",
                line={"width": 2},
                opacity=0.45,
            )
        )

        fig.add_trace(
            go.Scatter(
                x=sdf["timestamp"],
                y=sdf["value_ma"],
                mode="lines",
                name=f"{sensor} Filtered Data [°C]",
                line={"width": 2},
            )
        )

    fig.update_layout(
        yaxis_title="Process Value [°C]",
        template="plotly_white",
        hovermode="x unified",
        legend=dict(
            orientation="h",
            yanchor="bottom",
            y=1.08,
            xanchor="left",
            x=0,
        ),
        margin=dict(l=40, r=20, t=90, b=40),
        height=420,
    )
    fig.update_xaxes(
        title_text="Timestamp",
        tickformat="%H:%M:%S",
        hoverformat="%H:%M:%S",
    )
    return fig


def stats_table(df: pd.DataFrame) -> pd.DataFrame:
    out = (
        df.groupby("sensor")["value"]
        .agg(["mean", "std", "min", "max"])
        .reset_index()
        .rename(columns={"sensor": "Sensor", "mean": "Avg", "std": "Std_Dev", "min": "Min", "max": "Max"})
    )
    return out.round(2)


def build_history_table(df: pd.DataFrame) -> pd.DataFrame:
    history_df = df.copy()
    history_df["timestamp"] = pd.to_datetime(history_df["timestamp"])
    history_df["sensor_label"] = history_df["sensor"].astype(str)

    table_df = (
        history_df.pivot_table(
            index="timestamp",
            columns="sensor_label",
            values="value",
            aggfunc="last",
        )
        .reset_index()
        .sort_values("timestamp", ascending=False)
    )

    sensor_columns = [c for c in table_df.columns if c != "timestamp"]
    table_df = table_df[["timestamp"] + sensor_columns]
    table_df["timestamp"] = table_df["timestamp"].dt.strftime("%Y-%m-%d %H:%M:%S")
    return table_df.round(2)


app: Dash = dash.Dash(__name__)
app.title = "Air Heater Data Analysis Dashboard"

app.layout = html.Div(
    [
        dcc.Interval(id="refresh-timer", interval=REFRESH_MS, n_intervals=0),
        html.Div(
            [
                html.H1("Air Heater Data Analysis Dashboard", className="page-title"),
            ],
            className="header-block",
        ),
        html.Div(id="page-content"),
    ],
    className="app-shell",
    style={"backgroundColor": "#f9f6f6", "minHeight": "100vh"},
)


@app.callback(
    Output("page-content", "children"),
    Input("refresh-timer", "n_intervals"),
)
def render_page(_n_intervals: int):
    df = get_data(use_sql_server=True)

    if df.empty:
        return html.Div("No data available from SQL Server")

    snapshot = latest_snapshot(df)
    live_df = build_live_data_table(snapshot)
    stats_df = df[~df["sensor"].astype(str).str.contains("setpoint", case=False, na=False)]
    table_df = stats_table(stats_df)

    cutoff = datetime.now() - timedelta(hours=8)
    hdf = df[df["timestamp"] >= cutoff].copy()
    hdf.sort_values(["timestamp", "sensor"], ascending=[False, True], inplace=True)
    hdf = build_history_table(hdf)

    return html.Div(
        [
            html.Div(
                [
                    html.H3("Live Data", style={"marginBottom": "12px"}),
                    dash_table.DataTable(
                        data=live_df.to_dict("records"),
                        columns=[{"name": c, "id": c} for c in live_df.columns],
                        style_table={"overflowX": "auto"},
                        style_cell={
                            "textAlign": "left",
                            "padding": "10px",
                            "fontFamily": "Arial",
                        },
                        style_header={"fontWeight": "bold"},
                        style_cell_conditional=[
                            {
                                "if": {"column_id": "Signal"},
                                "width": "260px",
                                "minWidth": "260px",
                                "maxWidth": "260px",
                                "whiteSpace": "normal",
                            },
                            {
                                "if": {"column_id": "Value"},
                                "width": "100px",
                                "minWidth": "100px",
                                "maxWidth": "100px",
                            },
                            {
                                "if": {"column_id": "Last Updated"},
                                "width": "180px",
                                "minWidth": "180px",
                                "maxWidth": "180px",
                                "whiteSpace": "normal",
                            },
                        ],
                        style_data_conditional=[
                            {"if": {"row_index": "odd"}, "backgroundColor": "#fafafa"},
                        ],
                    ),
                ],
                className="section-panel",
                style={"marginBottom": "16px"},
            ),
            html.Div(
                [
                    html.H3("Control System Trend", style={"marginBottom": "10px"}),
                    dcc.Graph(
                        figure=control_system_figure(df.tail(300)),
                        style={"height": "100%"},
                        config={"displayModeBar": False},
                    ),
                ],
                className="section-panel",
                style={"minHeight": "460px", "marginTop": "12px"},
            ),
            html.Div(
                [
                    html.H3("Statistics"),
                    dash_table.DataTable(
                        data=table_df.to_dict("records"),
                        columns=[{"name": c, "id": c} for c in table_df.columns],
                        style_table={"overflowX": "auto"},
                        style_cell={
                            "textAlign": "left",
                            "padding": "8px",
                            "fontFamily": "Arial",
                        },
                        style_header={"fontWeight": "bold"},
                        style_cell_conditional=[
                            {
                                "if": {"column_id": "sensor"},
                                "width": "110px",
                                "minWidth": "110px",
                                "maxWidth": "110px",
                                "whiteSpace": "normal",
                            },
                        ],
                    ),
                ],
                className="section-panel",
            ),
            html.Div(
                [
                    html.H3("Recent History (Last 8 Hours)"),
                    dash_table.DataTable(
                        data=hdf.to_dict("records"),
                        columns=[
                            {"name": "Timestamp", "id": "timestamp"},
                            {"name": "Sensor 1", "id": "Air Heater 1, Sensor 1"},
                            {"name": "Operator Setpoint", "id": "Air Heater 1, Operator Setpoint"},
                        ],
                        page_size=20,
                        sort_action="native",
                        filter_action="native",
                        style_table={"overflowX": "auto"},
                        style_cell={
                            "textAlign": "left",
                            "padding": "8px",
                            "fontFamily": "Arial",
                        },
                        style_header={"fontWeight": "bold"},
                        style_cell_conditional=[
                            {
                                "if": {"column_id": "timestamp"},
                                "width": "170px",
                                "minWidth": "170px",
                                "maxWidth": "170px",
                                "whiteSpace": "normal",
                            },
                            {
                                "if": {"column_id": "Air Heater 1, Sensor 1"},
                                "width": "110px",
                                "minWidth": "110px",
                                "maxWidth": "110px",
                                "whiteSpace": "normal",
                            },
                            {
                                "if": {"column_id": "Air Heater 1, Operator Setpoint"},
                                "width": "90px",
                                "minWidth": "90px",
                                "maxWidth": "90px",
                            },
                        ],
                    ),
                ],
                className="section-panel",
            ),
        ]
    )


if __name__ == "__main__":
    if hasattr(app, "run"):
        app.run(debug=True)
    else:
        app.run_server(debug=True)
