# CIVIC 点胶时间检查

工位查询工具：扫 **Top Housing / Top Cover 主码**，或扫 **MMB / Heatsink / BBB / CSB 物料码**，按配对点胶站取出时间，只判定 **能不能清胶**。不要拆除。

配套提取器从 Housing、Cover **两个报文根**同时拷 `Request*.xml` 到本地。只要窗口版，没有 Windows 服务。

| 程序 | 路径 |
|---|---|
| 检查器 | `publish/CivicTimeChecker.exe` |
| 提取器 | `extractor-publish/CivicRequestExtractor.exe` |

GitHub：`DDZ-K/civic-time-checker`。Flex 那套仓库不改。

## 查询路由

| 扫到的码 | 用哪段点胶时间 |
|---|---|
| Top Housing 主码 | 25 和 60 都有 → `timeStamp` 更近的那段；只有一段 → 用那一段 |
| MMB PCB、Heatsink | 30 反查 Housing → **25** |
| BBB PCB | 70 反查 Housing → **60** |
| Top Cover 主码 | **100** |
| CSB PCB | 110 反查 Cover → **100** |

EMC 不做需求。胶筒码不扫。没报文，或只绑到组装、对应点胶站没时间 → 大卡 **未找到**，不记返工。

判定：已过 **≥** 该点胶站清胶分钟 → 可以清胶。默认 15 分钟，25 / 60 / 100 各自可配。

## 界面

- AntdUI 浅色检查器。顶栏只有 **配置 + 深色 BOSCH + 时钟**，BOSCH 旁不写产品名。
- 搜索框和查询按钮同高。主界面不显示报文目录路径。标题下没有小字。
- 一张「能否清胶」大卡。
- 底下三个返工圆环：25、60、100。口径同 Flex v1.3.6：产量看该点胶文件夹里还在的主码；返工看这次用的那份点胶报文落在哪个文件夹，再和还在的主码取交；提取器删报文就减；**不过零点清零**；数字变了原地改，不闪屏。

提取器顶栏同样是配置 + BOSCH + 时钟。配置里固定两格：**报文目录1**（Housing 55165）、**报文目录2**（Cover 55265）。

## 工位怎么开

1. 先开提取器，配两个报文根、六个站位保存路径（0025 / 0030 / 0060 / 0070 / 0100 / 0110），点开始提取。窗口要一直开着。
2. 再开检查器，把六个本地保存路径填进对应站。清胶分钟只填三个点胶站。
3. 扫码回车。

本机开发电脑不装服务、不加开机自启。

## 长时间运行

- 检查器每 10 分钟清空 XML 解析缓存，返工圆环每 60 秒刷新。
- 提取器每 10 分钟清理已处理文件表，源文件没了就丢掉对应缓存。
- 界面日志封顶（检查器 400 行，提取器 500 行）。
- **不要** `GC.Collect`。

## 自测

```bat
dotnet run --project CivicRequestExtractor -- --self-test
dotnet run --project CivicTimeChecker -- --self-test
```

退出码 0 才算过。
