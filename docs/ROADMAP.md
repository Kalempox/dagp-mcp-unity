# Roadmap

## Phase 0 — Skeleton ✅ (this commit)

- Repo scaffold, MIT license, credits to upstream `mcp-unity`
- Unity UPM package `com.dagp.unity-mcp`
- WebSocket bridge (`HttpListener` + `System.Net.WebSockets`)
- Main-thread dispatcher
- Tool registry with reflection-based auto-discovery
- Node MCP server (`@modelcontextprotocol/sdk` + `ws`)
- One tool: `ping`
- End-to-end test doc

## Phase 1 — Base toolset (~22 tools)

Match feature parity with `com.gamelovers.mcp-unity` v1.2.0.

| Group | Tools |
|---|---|
| Scene | `scene.get_info` `scene.create` `scene.load` `scene.save` `scene.unload` `scene.delete` |
| GameObject | `gameobject.get` `gameobject.add_asset` `gameobject.delete` `gameobject.duplicate` `gameobject.reparent` `gameobject.update` `gameobject.select` |
| Transform | `transform.set` `transform.move` `transform.rotate` `transform.scale` |
| Material | `material.create` `material.modify` `material.assign` `material.get_info` |
| Component | `component.update` |
| Project | `project.add_package` `project.recompile` `project.create_prefab` |
| Editor | `editor.execute_menu_item` `editor.run_tests` |
| Console | `console.get_logs` `console.send_log` |
| Batch | `batch.execute` |

## Phase 2 — Visual capture (★★★)

| Tool | Returns |
|---|---|
| `capture.game_view` | PNG path, resolution, timestamp |
| `capture.scene_view` | PNG path, scene-view camera pose |
| `capture.aspect_matrix` | Array of {aspect, path} for UI overflow detection |
| `capture.diff` | Pixel diff stats vs reference image |
| `render.get_visible_renderers` | List of Renderer names actually drawing in camera frustum |
| `render.get_canvas_layout` | Per-RectTransform world rect, anchors, overflow flags |
| `render.get_overdraw_stats` | Z-fighting hotspots, overdraw heatmap |

## Phase 3 — Play mode + input (★★★)

| Tool | Use |
|---|---|
| `play.enter` `play.exit` `play.pause` `play.step_frame` `play.is_playing` | Control loop |
| `input.click(x,y)` `input.drag(from,to,duration)` `input.key(code,hold)` `input.swipe(dir,len)` | Simulate user input |
| `runtime.wait_for_event(name,timeout)` | Block until game emits a named event |
| `runtime.get_gameobject_state(name,fields[])` | Read MonoBehaviour serialized fields at runtime |

## Phase 4 — Runtime probe + perf (★★)

| Tool | Returns |
|---|---|
| `runtime.get_animator_state(name)` | Current animator state, normalized time |
| `runtime.get_particle_state(name)` | isPlaying, particle count |
| `runtime.get_audio_playing()` | List of AudioSources currently playing |
| `perf.get_frame_stats(duration_s)` | FPS min/avg/max, GC alloc, draw calls, set-pass calls |
| `perf.start_profiler` `perf.stop_profiler(path)` | Profiler export |
| `validate.references` | Missing serialized refs, broken prefab refs, missing addressable keys |
| `build.player(target,path)` | Headless build for CI |

## Phase 5 — DAGP workflow integration

- `/sprint-plan` Unity branch (Phase A.5 smoke gate uses `ping` + `capture.game_view`)
- `qa-unity-visual` agent (consumes `capture.aspect_matrix`)
- `qa-unity-playtest` agent (consumes `play.*` + `input.*` + `runtime.*`)
- `qa-functional` + `qa-performance` Unity-aware blocks
- Verifiers: `verifier-unity-missing-refs`, `verifier-unity-scene-saved`
- Templates + docs

## Non-goals (for now)

- Multi-client connections (one MCP host per Unity Editor)
- Multi-Unity-Editor connections (one Editor per Node server)
- Headless Unity (no display, no game-view) — possible in Phase 4 with offscreen RenderTexture, but not Phase 0-3
