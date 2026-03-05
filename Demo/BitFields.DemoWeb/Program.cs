using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<BitFields.DemoWeb.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var host = builder.Build();

// Mark successful boot so the index.html loader knows WASM works in this
// browser. Edge Balanced users will auto-load on subsequent visits instead
// of seeing the welcome page again. Replaces the 'loading' flag that was
// set before the WASM load attempt.
var js = host.Services.GetRequiredService<IJSRuntime>();
await js.InvokeVoidAsync("localStorage.setItem", "blazorBoot", "success");

await host.RunAsync();
