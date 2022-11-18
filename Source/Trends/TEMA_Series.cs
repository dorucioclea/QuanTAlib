﻿namespace QuanTAlib;
using System;
using System.Linq;

/* <summary>
TEMA: Triple Exponential Moving Average
    TEMA uses EMA(EMA(EMA())) to calculate less laggy Exponential moving average.

Sources:
    https://www.tradingtechnologies.com/help/x-study/technical-indicator-definitions/triple-exponential-moving-average-tema/

Remark:
    ema1 = EMA(close, length)
    ema2 = EMA(ema1, length)
    ema3 = EMA(ema2, length)
    TEMA = 3 * (ema1 - ema2) + ema3

</summary> */

public class TEMA_Series : Single_TSeries_Indicator
{
    private readonly System.Collections.Generic.List<double> _buffer = new();
    private readonly double _k, _k1m;
    private double _lastema1, _lastlastema1;
    private double _lastema2, _lastlastema2;
    private double _lastema3, _lastlastema3;

    public TEMA_Series(TSeries source, int period, bool useNaN = false) : base(source, period, useNaN)
    {
        this._k = 2.0 / (this._p + 1);
        this._k1m = 1.0 - this._k;
        if (_data.Count > 0) { base.Add(_data); }
    }

    public override void Add((DateTime t, double v) TValue, bool update)
    {
        if (update)
        {
            this._lastema1 = this._lastlastema1;
            this._lastema2 = this._lastlastema2;
            this._lastema3 = this._lastlastema3;
        }

        double _ema1, _ema2, _ema3;

        if (this.Count < this._p)
        {
            Add_Replace_Trim(_buffer, TValue.v, _p, update);
            double _sma = _buffer.Average();
            _ema1 = _ema2 = _ema3 = _sma;
        }
        else
        {
            _ema1 = (TValue.v * this._k) + (this._lastema1 * this._k1m);
            _ema2 = (_ema1 * this._k) + (this._lastema2 * this._k1m);
            _ema3 = (_ema2 * this._k) + (this._lastema3 * this._k1m);
        }

        double _tema = (3 * (_ema1 - _ema2)) + _ema3;

        this._lastlastema1 = this._lastema1;
        this._lastlastema2 = this._lastema2;
        this._lastlastema3 = this._lastema3;
        this._lastema1 = _ema1;
        this._lastema2 = _ema2;
        this._lastema3 = _ema3;

        base.Add((TValue.t, _tema), update, _NaN);
        }
}