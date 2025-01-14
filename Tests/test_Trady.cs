using Xunit;
using Trady.Analysis.Indicator;
using Trady.Core;
using Trady.Core.Infrastructure;
using QuanTAlib;

public class TradyTests
{
    private readonly TBarSeries bars;
    private readonly GbmFeed feed;
    private Random rnd;
    private readonly double range;
    private int period, iterations;
    private int skip;
     private IEnumerable<IOhlcv> Candles;

    public TradyTests()
    {
        rnd = new((int)DateTime.Now.Ticks);
        feed = new(sigma: 0.5, mu: 0.0);
        bars = new(feed);
        range = 1e-9;
        feed.Add(10000);
        iterations = 3;
        skip = 500;
        Candles = bars.Select(bar => new Candle(
            bar.Time,
            (decimal)bar.Open,
            (decimal)bar.High,
            (decimal)bar.Low,
            (decimal)bar.Close,
            (decimal)bar.Volume
        )).ToList();
    }

    [Fact]
    public void SMA()
    {
        for (int run = 0; run < iterations; run++)
        {
            period = rnd.Next(50) + 5;
            Sma ma = new(period);
            TSeries QL = new();
            foreach (TBar item in feed)
            { QL.Add(ma.Calc(new TValue(item.Time, item.Close))); }

           var Trady = new SimpleMovingAverage(Candles, period)
                .Compute()
                .Select(result => new
                {
                    Date = result.DateTime,
                    Value = result.Tick.HasValue ? (double)result.Tick.Value : double.NaN
                })
                .ToList();

            Assert.Equal(QL.Length, Trady.Count);
            for (int i = QL.Length - 1; i > skip; i--)
            {
                double QL_item = QL[i].Value;
                double Tr_item =  Trady[i].Value;
                Assert.InRange(Tr_item - QL_item, -range, range);
            }
        }
    }

    [Fact]
    public void EMA()
    {
        for (int run = 0; run < iterations; run++)
        {
            period = rnd.Next(50) + 5;
            Ema ma = new(period);
            TSeries QL = new();
            foreach (TBar item in feed)
            { QL.Add(ma.Calc(new TValue(item.Time, item.Close))); }

           var Trady = new ExponentialMovingAverage(Candles, period)
                .Compute()
                .Select(result => new
                {
                    Date = result.DateTime,
                    Value = result.Tick.HasValue ? (double)result.Tick.Value : double.NaN
                })
                .ToList();

            Assert.Equal(QL.Length, Trady.Count);
            for (int i = QL.Length - 1; i > skip*2; i--)
            {
                double QL_item = QL[i].Value;
                double Tr_item =  Trady[i].Value;
                Assert.InRange(Tr_item - QL_item, -range, range);
            }
        }
    }


}