function setTargetObjDir(outDir)
	for _, cfg in ipairs("%{cfg.buildcfg}") do
		for _, plat in ipairs("%{cfg.platform}") do
			local action = _ACTION or ""
			
			local prj = project()
			
			--"_debug_win32_vs2008"
			local suffix = "_" .. cfg .. "_" .. plat .. "_" .. action
			
			targetPath = outDir
			
			suffix = string.lower(suffix)

			local obj_path = "../intermediate/" .. cfg .. "/" .. action .. "/" .. prj.name
			
			obj_path = string.lower(obj_path)
			
			configuration {cfg, plat}
				targetdir(targetPath)
				objdir(obj_path)
				targetsuffix(suffix)
		end
	end
end

function linkLib(libBaseName)
	for _, cfg in ipairs(configurations()) do
		for _, plat in ipairs(platforms()) do
			local action = _ACTION or ""
			
			local prj = project()
			
			local cfgName = cfg
			
			--"_debug_win32_vs2008"
			local suffix = "_" .. cfgName .. "_" .. plat .. "_" .. action
			
			libFullName = libBaseName .. string.lower(suffix)
			
			configuration {cfg, plat}
				links(libFullName)
		end
	end
end

workspace "test"
	configurations { "debug", "release" }
	platforms { "x32", "x64" }

	location ("./" .. (_ACTION or ""))
	language "C#"

	configuration "debug"
		defines { "DEBUG" }
		symbols "On"

	configuration "release"
		defines { "NDEBUG" }
		optimize "On"

	configuration "vs*"
		defines { "_CRT_SECURE_NO_WARNINGS" }
		
	configuration "gmake"
		buildoptions "-msse4.2"

	project "unittest"
		kind "ConsoleApp"
		
		files { 
			"../src/csharp/**.cs",
			"../test/unittest/**.cs",
		}
		
		setTargetObjDir("../bin")

	project "perftest"
		kind "ConsoleApp"
		
		files { 
			"../src/csharp/**.cs",
			"../test/perftest/**.cs",
		}
		
		setTargetObjDir("../bin")

workspace "example"
	configurations { "debug", "release" }
	platforms { "x32", "x64" }
	location ("./" .. (_ACTION or ""))
	language "C#"

	configuration "debug"
		defines { "DEBUG" }
		symbols "On"

	configuration "release"
		defines { "NDEBUG" }
		optimize "On"
		vectorextensions "SSE2"

	configuration "vs*"
		defines { "_CRT_SECURE_NO_WARNINGS" }

	project "example"
		kind "ConsoleApp"
		files {
			"../src/csharp/**.cs",
			"../example/*",
		}

		setTargetObjDir("../bin")

