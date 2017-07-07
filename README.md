# SLua
Fastest lua binding via static code generation for Unity3D or mono

## Release Download

- [slua-unity](https://github.com/braedonwooding/slua/releases/latest)

## Help

See [Wiki](https://github.com/BraedonWooding/slua/wiki).

## Main features

- static code generation, no reflection, no extra gc alloc, very fast
- remote debugger
- full support iOS/iOS64, support il2cpp
- above 90% UnityEngine interface exported ( remove flash, platform dependented interface )
- 100% UnityEngine.UI interface ( require Unity4.6+ )
- support standalone mode in .net framework/mono without Unity3D
- support UnityEvent/UnityAction for event callback via lua function
- support delegate via lua function (include iOS)
- support yield call
- support custom class exported
- support extension method
- export enum as integer
- return array as lua table
- using raw luajit, can be replaced with lua5.3/lua5.1

## Usage

1) Copy Assets/Plugins and Assets/Slua to your $Project$/Assets folder and let it compile
2) Click Slua/Unity/Make UnityEngine to regenerate the UnityEngine interface for lua
3) Click Slua/Unity/Make UI to regenerate the UnityEngine.UI interface for lua
4) Click Slua/Custom/Make to generate custom class interfaces for lua

Precompiled slua library in Plugins only included x86(32bit)/macosx(32bit)/iOS(armv7,armv7s,arm64)/Android(armv7-a) platform using luajit, you should compile other platform/lua5.1/luajit by yourself, see build.txt for help.

## Usage at a glance
```lua
-- import
import "UnityEngine"

function main()

	-- create gameobject
	local cube = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube)

	-- find gameobject
	local go = GameObject.Find("Canvas/Button")
	
	-- get component by type name
	local btn = go:GetComponent("Button")
	
	-- get out parameter
	local ok,hitinfo = Physics.Raycast(Vector3(0,0,0),Vector3(0,0,1),Slua.out)
	print("Physics Hitinfo",ok,hitinfo)
	
	-- foreach enumeratable object
	for t in Slua.iter(Canvas.transform) do
		print("foreach transorm",t)
	end
	
	-- add event listener
	btn.onClick:AddListener(function()
		local go = GameObject.Find("Canvas/Text")
		local label = go:GetComponent("Text")
		label.text="hello world"
	end)
	
	-- use vector3
	local pos = Vector3(10,10,10)+Vector3(1,1,1)
	cube.transform.position = pos
	
	-- use coroutine
	local c=coroutine.create(function()
		print "coroutine start"

		Yield(WaitForSeconds(2))
		print "coroutine WaitForSeconds 2"

		local www = WWW("http://www.sineysoft.com")
		Yield(www)
		print(#Slua.ToString(www.bytes))
	end)
	coroutine.resume(c)

	-- add delegate
	Deleg.daction = {"+=",self.actionD} --it's ok for iOS
	
	-- remove delegate
	Deleg.daction = {"-=",self.actionD} --it's ok for iOS
	
	-- set delegate
	Deleg.daction = function() print("callback") end --it's ok for iOS
	
	-- remove all
	Deleg.daction = nil
end
```

##Export custom class
Add the CustomLuaClass attribute to your custom class, then click SLua/Custom/Make and you will get interface file for lua.
```c#
[CustomLuaClass]
public class HelloWorld   {
    ...
}
```

### Benchmark
(Insert Benchmarks)
