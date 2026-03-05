using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<BitFields.DemoWeb.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var host = builder.Build();

// Clear the crash-detection flag set by index.html. If we reach this point,
// the .NET WASM runtime loaded successfully and the app is running. 
var js = host.Services.GetRequiredService<IJSRuntime>();
await js.InvokeVoidAsync("localStorage.removeItem", "blazorBoot");

await host.RunAsync();
