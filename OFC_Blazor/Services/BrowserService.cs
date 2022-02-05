using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace OFC_Blazor.Services;

public class BrowserService
{
    private readonly IJSRuntime _js;

    public BrowserService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<int> GetProcessorCount()
    {
        // https://eligrey.com/blog/cpu-core-estimation-with-javascript/
        var ProcessorCount = await _js.InvokeAsync<int>("eval", "window.navigator.hardwareConcurrency;");
        return ProcessorCount;
    }

    public async Task<BrowserDimension> GetWindowDimensions()
    {
        try
        {
            //return await _js.InvokeAsync<BrowserDimension>("getDimensions");
            var result = await _js.InvokeAsync<BrowserDimension>("eval", @"let dims = {width: window.innerWidth, height: window.innerHeight}; dims;");
            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}

public class BrowserDimension
{
    public int Width { get; set; }
    public int Height { get; set; }
}