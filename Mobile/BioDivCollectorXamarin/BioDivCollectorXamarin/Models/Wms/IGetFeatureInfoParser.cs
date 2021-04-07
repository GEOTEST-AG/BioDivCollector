using System.IO;

namespace BioDivCollectorXamarin.Models.Wms
{
    public interface IGetFeatureInfoParser
    {
        FeatureInfo ParseWMSResult(string layerName, Stream result);
    }
}
