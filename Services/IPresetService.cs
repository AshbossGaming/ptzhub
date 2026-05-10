namespace ptz_hub.Services;

public interface IPresetService
{
    Task SaveAsync(string name, double pan, double tilt);
    Task<(double Pan, double Tilt)?> RecallAsync(string name);
    Task<bool> DeleteAsync(string name);
    Task<Dictionary<string, object>> ListAsync();
}
