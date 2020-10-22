using UnityEngine;

public class FloatStatistic
{
    public float Mean { get; private set; }
    public float Sigma { get; private set; }
    public int Count { get; private set; }

    public void AddValue(float value)
    {
        sum += value;
        sumSquared += value * value;
        Count++;

        Mean = (float)(sum / Count);
        var sigmaSq = (float)(sumSquared / Count - (Mean * Mean));
        var sigma = sigmaSq;
        if (sigmaSq > Mathf.Epsilon)
        {
            sigma = Mathf.Sqrt(sigmaSq);
        }
        Sigma = sigma;
    }

    double sum;
    double sumSquared;
}
