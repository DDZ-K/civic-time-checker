# CIVIC 点胶时间检查 — 项目上下文

新产品，不改 Flex 仓库。工程在 `E:\Download\AI\projects\civic-time-checker\`。

## 锁定口径

- 检查器 `CivicTimeChecker.exe`，提取器 `CivicRequestExtractor.exe`。
- 提取器只要界面，不要 Service、不要安装脚本、不要开机自启。
- 双报文根固定两格：WatchRoot1 / WatchRoot2。
- 六个本地目录：25/60/100 点胶 + 30/70/110 组装。分钟只挂点胶站。
- Housing 主码：25 与 60 取 `timeStamp` 最近一段。
- 物料：MMB/Heatsink → 25，BBB → 60，CSB → 100。
- Heatsink 在 30 的 `processData` 里，按 item value 反查。
- EMC 不作为需求。
- 清胶：已过 ≥ 分钟 → 可以。找不到 → 大卡「未找到」，不记返工。
- 返工圆环三个，口径照 Flex v1.3.6，不过零点清零。
- 壳：检查器 AntdUI 浅色、深色 BOSCH、顶栏配置+BOSCH+时钟。
- 长时间运行：10 分钟刷新缓存、日志封顶、禁止 GC.Collect。

## 样本码（桌面 CIVIC 报文）

- Housing `009756186180205070044250001`
  - 25：2026-09-23 15:26:41
  - 60：2026-09-23 15:29:39（主码查询应取这一段）
  - 30：MMB `69165000000480022608318629503833`，Heatsink `408665086180160350040250002`
  - 70：BBB `69165000000470022609028629503832`
- Cover `184898886180155130032250002`
  - 100：2026-09-23 15:27:35
  - 110：CSB `69157000000410022608318629503837`

现场报文根：

- Housing：`DirectDataLink_55165` … `Line_288` 下 0025/0030/0060/0070
- Cover：`DirectDataLink_55265` … `Line_288` 下 0100/0110

## 发布

```bat
dotnet publish CivicTimeChecker\CivicTimeChecker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
dotnet publish CivicRequestExtractor\CivicRequestExtractor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o extractor-publish
```
