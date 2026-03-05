using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<BitFields.DemoWeb.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var host = builder.Build();

// Mark successful boot so the index.html loader knows WASM works in this
// browser. Overrides the 'loading' or 'crashed' flag that was set before the
// WASM load attempt. Edge Balanced users will auto-load on all future visits.
var js = host.Services.GetRequiredService<IJSRuntime>();
await js.InvokeVoidAsync("localStorage.setItem", "blazorBoot", "success");

await host.RunAsync();
