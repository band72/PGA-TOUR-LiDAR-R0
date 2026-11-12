namespace PGA.Model.BLL
{
    public interface IModel
    {

        void WriteDataToRegistry(string Key, string Val);

        LayerStatesBLL GetDataFromRegistry();
    }
}