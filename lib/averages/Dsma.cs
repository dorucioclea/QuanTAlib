namespace QuanTAlib;

/// <summary>
/// DSMA: Deviation Scaled Moving Average
/// Adaptive moving average that adjusts its smoothing factor based on the volatility of the input data.
/// It aims to be more responsive during trending periods and more stable during ranging periods.
/// </summary>
/// <remarks>
/// Smoothness:     ★★★★☆ (4/5)
/// Sensitivity:    ★★★★☆ (4/5)
/// Overshooting:   ★★★★☆ (4/5)
/// Lag:            ★★★★☆ (4/5)
/// 
/// The DSMA uses a SuperSmoother filter to reduce noise and a dynamic alpha calculation based on the
/// scaled deviation of the input data. This allows it to adapt to changing market conditions.
///
/// The algorithm involves these main steps:
/// 1. Apply a SuperSmoother filter to the zero-mean input data.
/// 2. Calculate the Root Mean Square (RMS) of the filtered data.
/// 3. Scale the filtered data by the RMS to get a measure in terms of standard deviations.
/// 4. Use the scaled deviation to calculate an adaptive alpha for the moving average.
///
/// Source:
///    https://www.mesasoftware.com/papers/DEVIATION%20SCALED%20MOVING%20AVERAGE.pdf
/// </remarks>

public class Dsma : AbstractBase
{
    private readonly int _period;
    private readonly CircularBuffer _buffer;
    private readonly double _a1, _b1, _c1, _c2, _c3;
    private double _lastDsma, _p_lastDsma;
    private double _filt, _filt1, _filt2, _zeros, _zeros1;
    private double _p_filt, _p_filt1, _p_filt2, _p_zeros, _p_zeros1;
    private bool _isInit, _p_isInit;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dsma"/> class.
    /// </summary>
    /// <param name="period">The number of data points used in the DSMA calculation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when period is less than 1.</exception>
    public Dsma(int period) : base()
    {
        if (period < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than or equal to 1.");
        }
        _period = period;
        _buffer = new CircularBuffer(period);

        // SuperSmoother filter coefficients
        _a1 = Math.Exp(-1.414 * Math.PI / (0.5 * period));
        _b1 = 2 * _a1 * Math.Cos(1.414 * Math.PI / (0.5 * period));
        _c2 = _b1;
        _c3 = -_a1 * _a1;
        _c1 = 1 - _c2 - _c3;

        Name = "Dsma";
        WarmupPeriod = period * 2; // A conservative estimate
        Init();
    }

    public Dsma(object source, int period) : this(period)
    {
        var pubEvent = source.GetType().GetEvent("Pub");
        pubEvent?.AddEventHandler(source, new ValueSignal(Sub));
    }

    public override void Init()
    {
        base.Init();
        _lastDsma = 0;
        _filt = _filt1 = _filt2 = 0;
        _zeros = _zeros1 = 0;
        _isInit = false;
    }

    protected override void ManageState(bool isNew)
    {
        if (isNew)
        {
            _p_lastDsma = _lastDsma;
            _p_isInit = _isInit;
            _p_zeros = _zeros;
            _p_zeros1 = _zeros1;
            _p_filt = _filt;
            _p_filt1 = _filt1;
            _p_filt2 = _filt2;
            _index++;
        }
        else
        {
            _lastDsma = _p_lastDsma;
            _isInit = _p_isInit;
            _zeros = _p_zeros;
            _zeros1 = _p_zeros1;
            _filt = _p_filt;
            _filt1 = _p_filt1;
            _filt2 = _p_filt2;
        }
    }

    protected override double Calculation()
    {
        ManageState(Input.IsNew);

        if (!_isInit)
        {
            _lastDsma = Input.Value;
            _isInit = true;
            return _lastDsma;
        }

        // Produce nominal zero mean
        _zeros = Input.Value - _lastDsma;

        // SuperSmoother Filter
        _filt = _c1 * (_zeros + _zeros1) / 2 + _c2 * _filt1 + _c3 * _filt2;

        // Update buffer for RMS calculation
        _buffer.Add(_filt * _filt, Input.IsNew);

        // Compute RMS (Root Mean Square)
        double rms = Math.Sqrt(_buffer.Sum() / _period);

        // Rescale Filt in terms of Standard Deviations
        double scaledFilt = rms != 0 ? _filt / rms : 0;

        // Calculate adaptive alpha
        double alpha = Math.Abs(scaledFilt) * 5 / _period;

        // DSMA calculation
        double dsma = alpha * Input.Value + (1 - alpha) * _lastDsma;

        // Update state variables
        _zeros1 = _zeros;
        _filt2 = _filt1;
        _filt1 = _filt;
        _lastDsma = dsma;

        IsHot = _index >= WarmupPeriod;
        return dsma;
    }
}
