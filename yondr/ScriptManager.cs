using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

public class ScriptManager {
	
	public ScriptManager() {
		scripts = new List<MethodInfo>[(int)Event.COUNT];
		for (int i = 0; i < scripts.Length; i++) {
			scripts[i] = new List<MethodInfo>();
		}
		
		// Scripts only have execution permissions.
		var permissions = new PermissionSet(PermissionState.None);
		permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
		
		var helperType = typeof(ScriptHelper);
		var  helperStrongName = GetStrongName(helperType.Assembly);
		var contextStrongName = GetStrongName(typeof(IContext).Assembly);
		
		// Create appdomain.
		domain = AppDomain.CreateDomain(
			"ScriptDomain",
			new Evidence(),
			new AppDomainSetup(),
			permissions,
			new StrongName[] { helperStrongName, contextStrongName }
		);
		
		helper = (ScriptHelper)domain.CreateInstanceFromAndUnwrap("script-helper.dll",
		                                                          "ScriptHelper");
	}
	~ScriptManager() {
		AppDomain.Unload(domain);
	}
	
	private StrongName GetStrongName(Assembly ass) {
		AssemblyName assemblyName = ass.GetName();
		byte[] publicKey = assemblyName.GetPublicKey();
		if (publicKey == null || publicKey.Length == 0) {
			throw new InvalidOperationException(String.Format("{0} is not strongly named", ass));
		}
		var keyBlob     = new StrongNamePublicKeyBlob(publicKey);
		return new StrongName(keyBlob, assemblyName.Name, assemblyName.Version);
	}
	
	public void Compile(Res.Package package) {
		string outDir = "_" + package.Name + ".dll";
		Assembly ass = null;
		
		// Check if package is already compiled.
		if (File.Exists(outDir)) {
			DateTime outDate = File.GetLastWriteTime(outDir);
			DateTime pakDate = Directory.GetLastWriteTime(package.Path);
			if (pakDate < outDate) {
				ass = helper.Load(outDir);
			}
			if (ass != null) {
				Log.Info("Loaded {0}", outDir);
			}
		}
		if (ass == null) {
			var scripts = new List<string>();
			foreach (Res.Res res in package.Resources.Values) {
				if (res.Type == Res.Type.SCRIPT) {
					scripts.Add(res.Path);
				}
			}
			if (scripts.Count == 0) return;
			var result = helper.Compile(scripts.ToArray(), outDir);
			if (result.Errors.HasErrors) {
				Log.Error("Failed to compile {0}:", outDir);
				foreach (var error in result.Errors) {
					Log.Info("  {0}", error);
				}
				return;
			}
			Log.Info("Compiled {0}", outDir);
			foreach (var error in result.Errors) {
				Log.Info("  {0}", error);
			}
			foreach (var output in result.Output) {
				Log.Info(" {0}", output);
			}
		}
		
		var flags = BindingFlags.Public | BindingFlags.Static;
		
		var events = ass.GetType("Events", false);
		if (events != null) {
			foreach (MethodInfo meth in events.GetMethods(flags)) {
				Event even;
				switch (meth.Name) {
					case "Init":   even = Event.INIT;   break;
					case "Update": even = Event.UPDATE; break;
					case "Exit":   even = Event.EXIT;   break;
					default: {
						Log.Warn("{0} does not match any known events. " + 
						         "If it's a helper function, make it private.",
						         meth.Name);
						continue;
					}
				}
				var expectedParams = EventParameters[(int)even];
				var foundParams    = meth.GetParameters().Select((p, _) => p.ParameterType);
				if (!expectedParams.SequenceEqual(foundParams)) {
					Log.Error("'{0}' does not have the correct parameters.\n" + 
					          "  Expected: {1}\n  Found: {2}",
					          meth.Name, expectedParams, foundParams);
					continue;
				}
				scripts[(int)even].Add(meth);
			}
		}
	}
	
	public enum Event {
		INIT,
		UPDATE,
		EXIT,
		COUNT,
	}
	private Type[][] EventParameters = {
		new Type[] { typeof(IContext) },
		new Type[] { typeof(IContext), typeof(double) },
		new Type[] { typeof(IContext) },
	};
	
	public void Init() {
		foreach (MethodInfo meth in scripts[(int)Event.INIT]) {
			meth.Invoke(null, new object[] { context });
		}
	}
	public void Update(double diff) {
		foreach (MethodInfo meth in scripts[(int)Event.UPDATE]) {
			meth.Invoke(null, new object[] { context, diff });
		}
	}
	public void Exit() {
		foreach (MethodInfo meth in scripts[(int)Event.EXIT]) {
			meth.Invoke(null, new object[] { context });
		}
	}
	
	private readonly ScriptContext context = new ScriptContext();
	private AppDomain domain;
	private ScriptHelper helper;
	private readonly List<MethodInfo>[] scripts;
}
