using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZooSimulator.App.WPF
{
    /// <summary>Opt-in local verification of this app's own renderer; exposes no debugging port.</summary>
    internal static class SceneVerification
    {
        private const string readState = @"JSON.stringify({
            ready: !!window.zooDiagnostics,
            loading: document.getElementById('loading')?.innerText,
            hour: 'HOUR ' + String(document.getElementById('time')?.dataset.hours).padStart(2,'0'),
            clockText: document.getElementById('time')?.innerText,
            celebrating: document.getElementById('clock-mascot')?.classList.contains('excited'),
            clockAnimal: window.zooDiagnostics?.clockAnimal ? {
                clip:window.zooDiagnostics.clockAnimal.clip,
                heading:window.zooDiagnostics.clockAnimal.root.rotation.y,
                width:document.getElementById('mascot-scene').width,
                height:document.getElementById('mascot-scene').height
            } : null,
            foodCentreOffset: (() => { const r=document.querySelector('.foods').getBoundingClientRect(); return r.x+r.width/2-innerWidth/2; })(),
            fps: window.zooDiagnostics?.engine.getFps(),
            animals: window.zooDiagnostics ? [...window.zooDiagnostics.animals.values()].map(a => ({
                id:a.id, health:a.health, status:a.status, clip:a.clip,
                x:a.root.position.x, z:a.root.position.z, radius:a.radius, species:a.species,
                heading:a.root.rotation.y, scale:a.root.scaling.x,
                labelX:a.labelX, labelY:a.labelY, tail:a.tail.getAttribute('d'),
                low:a.label.classList.contains('low'), fed:a.label.classList.contains('fed'),
                hovered:a.label.classList.contains('hovered'), opacity:getComputedStyle(a.label).opacity,
                gainBadges:a.effects.querySelectorAll('.health-gain:not(:empty)').length,
                effectX:a.effectX, effectY:a.effectY, baseScale:a.baseScale,
                selectedMeshes:a.meshes.filter(m=>m.renderOverlay).length,
                sparkles:a.effects.querySelectorAll('.feed-burst i').length,
                sweatAnimations:[...a.effects.querySelectorAll('.sweat-drop')].reduce((n,d)=>n+d.getAnimations().length,0),
                uiAnimations:a.label.querySelector('.health-card').getAnimations().length
            })) : []
        })";

        internal static void Trace(string message)
        {
            string[] args = Environment.GetCommandLineArgs();

            int index = Array.IndexOf(args, "--verify-scene");

            if (index < 0 || index + 1 >= args.Length)
                return;

            Directory.CreateDirectory(args[index + 1]);

            File.AppendAllText(
                Path.Combine(args[index + 1], "host.log"),
                message + Environment.NewLine);
        }

        internal static async Task Run(CoreWebView2 browser, string directory)
        {
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);

            JObject initial = new JObject();

            for (int attempt = 0; attempt < 60; attempt++)
            {
                await Task.Delay(400);

                initial = await Read(browser);

                if (initial.Value<bool>("ready") && initial["animals"]!.Count() == 15)
                    break;
            }

            await Capture(browser, directory, "initial");

            File.WriteAllText(
                Path.Combine(directory, "initial.json"),
                initial.ToString());

            if (initial["animals"]!.Count() != 15)
                throw new InvalidOperationException(
                    "The scene did not produce fifteen animals; inspect initial.png and initial.json.");

            await Task.Delay(2500);

            var moved = await Read(browser);

            File.WriteAllText(
                Path.Combine(directory, "movement.json"),
                moved.ToString());

            string hitChecks = await browser.ExecuteScriptAsync(@"JSON.stringify((() => {
                const animals=[...window.zooDiagnostics.animals.values()], tests=[];
                for(const animal of animals) {
                    const r=animal.label.querySelector('.health-card').getBoundingClientRect();
                    for(const [x,y] of [[r.x+r.width/2,r.y+r.height/2],[r.left+1,r.top+1],[r.right-1,r.bottom-1]]) {
                        const visible=animals.filter(a=>{const b=a.label.querySelector('.health-card').getBoundingClientRect();return x>=b.left&&x<=b.right&&y>=b.top&&y<=b.bottom;}).at(-1);
                        tests.push({expected:visible.id,actual:window.zooDiagnostics.pick(x,y)?.id});
                    }
                }
                return tests;
            })())");

            File.WriteAllText(
                Path.Combine(directory, "health-hit-tests.json"),
                JsonConvert.DeserializeObject<string>(hitChecks));

            await browser.ExecuteScriptAsync(@"(() => {
                const animal=[...window.zooDiagnostics.animals.values()].find(a=>a.health===100);
                window.zooHoverTestId=animal.id;
                const r=animal.label.getBoundingClientRect();
                document.getElementById('scene').dispatchEvent(new PointerEvent('pointermove',{bubbles:true,pointerId:99,clientX:r.x+r.width/2,clientY:r.y+r.height/2}));
            })()");

            await Task.Delay(400);

            File.WriteAllText(
                Path.Combine(directory, "healthy-hover.json"),
                (await Read(browser)).ToString());

            await browser.ExecuteScriptAsync(
                "document.dispatchEvent(new PointerEvent('pointerleave',{pointerId:99}))");

            await Task.Delay(1000);

            File.WriteAllText(
                Path.Combine(directory, "healthy-hover-ended.json"),
                (await Read(browser)).ToString());

            // Exercise a real twenty-second hourly transition through the running application.
            for (int attempt = 0; attempt < 65 && moved.Value<string>("hour") == "HOUR 00"; attempt++)
            {
                await Task.Delay(400);

                moved = await Read(browser);
            }

            File.WriteAllText(
                Path.Combine(directory, "hour.json"),
                moved.ToString());

            await Capture(browser, directory, "hour");

            var target = moved["animals"]!.First(
                animal => animal.Value<double>("health") < 99);

            int id = target.Value<int>("id");

            await browser.ExecuteScriptAsync(
                "(() => { const target=document.querySelector('[data-animal-id=\"" + id + "\"]');" +
                "const r=target.getBoundingClientRect(), food=document.querySelector('.food'), f=food.getBoundingClientRect();" +
                "const p={bubbles:true,pointerId:17,pointerType:'mouse',isPrimary:true};" +
                "food.dispatchEvent(new PointerEvent('pointerdown',{...p,clientX:f.x+f.width/2,clientY:f.y+f.height/2}));" +
                "document.dispatchEvent(new PointerEvent('pointermove',{...p,clientX:r.x+r.width/2,clientY:r.y+r.height/2})); })()");

            await Task.Delay(80);

            File.WriteAllText(Path.Combine(directory, "hover.json"), (await Read(browser)).ToString());

            await Capture(browser, directory, "hover");

            await browser.ExecuteScriptAsync(
                "(() => { const r=document.querySelector('.animal-label[data-animal-id=\"" + id + "\"]').getBoundingClientRect();" +
                "document.dispatchEvent(new PointerEvent('pointerup',{bubbles:true,pointerId:17,pointerType:'mouse',isPrimary:true,clientX:r.x+r.width/2,clientY:r.y+r.height/2})); })()");

            await Task.Delay(180);

            File.WriteAllText(Path.Combine(directory, "feeding-feedback.json"), (await Read(browser)).ToString());

            await Capture(browser, directory, "feeding-feedback");

            await Task.Delay(520);

            var targeted = await Read(browser);

            File.WriteAllText(Path.Combine(directory, "targeted.json"), targeted.ToString());

            await browser.ExecuteScriptAsync("document.getElementById('feed-all').click()");

            await Task.Delay(700);

            var fed = await Read(browser);

            File.WriteAllText(Path.Combine(directory, "fed.json"), fed.ToString());

            await Capture(browser, directory, "fed");

            await browser.ExecuteScriptAsync("document.getElementById('reset-game').click()");

            await Task.Delay(200);

            File.WriteAllText(
                Path.Combine(directory, "reset-growing.json"),
                (await Read(browser)).ToString());

            await Task.Delay(600);

            var reset = await Read(browser);

            File.WriteAllText(
                Path.Combine(directory, "reset.json"),
                reset.ToString());

            await Capture(browser, directory, "reset");

            if (reset.Value<string>("hour") != "HOUR 00" ||
                reset["animals"]!.Any(
                    animal => animal.Value<double>("health") != 100))
            {
                throw new InvalidOperationException(
                    "Reset did not restore hour zero and full animal health.");
            }

            // Sample the per-frame limit rather than relying only on a screenshot of a turn.
            await browser.ExecuteScriptAsync(@"(() => {
                const samples=window.zooMotionSamples=[]; let previous=null; let count=0;
                function sample(now) {
                    const current=[...window.zooDiagnostics.animals.values()].map(a=>({id:a.id,yaw:a.root.rotation.y,x:a.labelX,y:a.labelY}));
                    if(previous) samples.push({dt:(now-previous.now)/1000,animals:current.map((a,i)=>({id:a.id,turn:Math.abs(a.yaw-previous.animals[i].yaw),label:Math.hypot(a.x-previous.animals[i].x,a.y-previous.animals[i].y)}))});
                    previous={now,animals:current};
                    if(++count<90) requestAnimationFrame(sample);
                } requestAnimationFrame(sample);
            })()");

            await Task.Delay(2000);

            string motion = await browser.ExecuteScriptAsync("JSON.stringify(window.zooMotionSamples)");

            File.WriteAllText(
                Path.Combine(directory, "motion.json"),
                JsonConvert.DeserializeObject<string>(motion));

            // Presentation-only probe: core survival rules are covered by domain tests.
            await browser.ExecuteScriptAsync(@"(() => {
                const animals=[...window.zooDiagnostics.animals.values()].map(a=>({id:a.id,species:a.species,health:a.health,status:a.status}));
                animals.find(a=>a.id===3).status=1; animals.find(a=>a.id===3).health=60;
                animals.find(a=>a.id===6).status=2; animals.find(a=>a.id===6).health=40;
                window.postMessage({version:1,actor:'zoo',operation:'show',data:{revision:999998,generation:1,hoursElapsed:0,animals}},location.origin);
            })()");

            await Task.Delay(500);

            await Capture(browser, directory, "presentation-states");

            File.WriteAllText(
                Path.Combine(directory, "presentation-states.json"),
                (await Read(browser)).ToString());

            await browser.ExecuteScriptAsync(@"(() => {
                const stats=window.zooResizeStats={frames:0,outOfBounds:0,renderMilliseconds:[],lowAnimations:document.querySelector('.animal-label.low .bar span')?.getAnimations().length};
                const world=document.getElementById('world');
                window.zooResizeObserver=window.zooDiagnostics.scene.onAfterRenderObservable.add(() => {
                    stats.frames++;
                    stats.renderMilliseconds.push(window.zooDiagnostics.engine.getDeltaTime());
                    const bounds=world.getBoundingClientRect();
                    document.querySelectorAll('.health-card').forEach(label=>{
                        const r=label.getBoundingClientRect();
                        if(r.left<bounds.left-1 || r.right>bounds.right+1 || r.top<bounds.top-1 || r.bottom>bounds.bottom+1) stats.outOfBounds++;
                    });
                });
            })()");

            var window = System.Windows.Application.Current.MainWindow;

            for (int step = 0; step < 18; step++)
            {
                window.Width = 1000 + (step % 6) * 80;
                window.Height = 720 + (step % 4) * 60;

                await Task.Delay(100);
            }

            window.Width = 1440;
            window.Height = 940;

            await Task.Delay(600);

            string resizeStats = await browser.ExecuteScriptAsync(
                "JSON.stringify(window.zooResizeStats)");

            await browser.ExecuteScriptAsync(
                "window.zooDiagnostics.scene.onAfterRenderObservable.remove(window.zooResizeObserver)");

            File.WriteAllText(
                Path.Combine(directory, "resize.json"),
                JsonConvert.DeserializeObject<string>(resizeStats));

            await Capture(browser, directory, "resized");
        }

        private static async Task<JObject> Read(CoreWebView2 browser)
        {
            string encoded = await browser.ExecuteScriptAsync(readState);

            string json = JsonConvert.DeserializeObject<string>(encoded)
                ?? "{}";

            return JObject.Parse(json);
        }

        private static async Task Capture(CoreWebView2 browser, string directory, string name)
        {
            using var output = File.Create(
                Path.Combine(directory, name + ".png"));

            await browser.CapturePreviewAsync(
                CoreWebView2CapturePreviewImageFormat.Png, output);
        }
    }
}
