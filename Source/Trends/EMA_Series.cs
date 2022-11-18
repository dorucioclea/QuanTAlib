﻿namespace QuanTAlib;
using System;
using System.Linq;

/* <summary>
EMA: Exponential Moving Average
    EMA needs very short history buffer and calculates the EMA value using just the
    previous EMA value. The weight of the new datapoint (k) is k = 2 / (period-1)

Sources:
    https://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_averages
    https://www.investopedia.com/ask/answers/122314/what-exponential-moving-average-ema-formula-and-how-ema-calculated.asp
    https://blog.fugue88.ws/archives/2017-01/The-correct-way-to-start-an-Exponential-Moving-Average-EMA

Issues:
    There is no consensus what the first EMA value should be - a zero, a first
    datapoint, or an average of the initial Period bars. All three starting methods
    converge within 20+ bars to the same moving average. Most implementations (including this one)
    use SMA() for the first Period bars as a seeding value for EMA.

</summary> */

public class EMA_Series : Single_TSeries_Indicator
{
    private readonly System.Collections.Generic.List<double> _buffer = new();
    private readonly double _k, _k1m;
    private double _lastema, _lastlastema;

    public EMA_Series(TSeries source, int period, bool useNaN = false) : base(source, period, useNaN)
    {
        this._k = 2.0 / (this._p + 1);
        this._k1m = 1.0 - this._k;
        this._lastema = this._lastlastema = double.NaN;
        if (this._data.Count > 0) { base.Add(this._data); }
    }

    public override void Add((DateTime t, double v) TValue, bool update)
    {
        double _ema;
        if (update) { this._lastema = this._lastlastema; }

        if (this.Count < this._p)
        {
            Add_Replace(_buffer, TValue.v, update);
            _ema = _buffer.Average();
        }
        else
        {
            _ema = (TValue.v * this._k) + (this._lastema * this._k1m);
        }

        this._lastlastema = this._lastema;
        this._lastema = _ema;

        base.Add((TValue.t, _ema), update, _NaN);
    }
}