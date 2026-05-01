# lfEDL (Qualcomm)
## 仅测试部分功能 未测试完整读写 部分内容可能已经过时！
lfEDL 是一个基于 Avalonia UI 的跨平台高通 9008 (EDL) 刷机工具。
本项目从 SakuraEDL 分离重构，专注于高通平台的刷写、分区管理和高级认证功能。

## 功能特性

- **跨平台 UI**: 使用 Avalonia UI 框架，支持 Windows (x64) 运行。
- **Firehose 协议**: 完整实现高通 Firehose XML 刷写协议，支持 UFS/eMMC。
- **分区管理**:
  - 读取 GPT 分区表 (支持多 LUN 解析)
  - 分区读写、擦除操作
  - 保护分区列表 (防止误删关键数据)
- **高级认证**:
  - 小米设备免授权 (Sysmtem.Security.Cryptography) / 交互式签名认证弹窗
  - OnePlus 项目 ID 认证
  - VIP 认证 (Loader/Digest/Signature 注入)
- **实用工具**:
  - 自动检测并刷新端口 (Port Monitor)
  - XML 自动加载与解析
  - 双进度条显示 (总进度/子任务) 及实时速度统计
  - 详细的日志系统 (UI 日志 + 文件日志)

## 项目结构

- **lfEDL.Avalonia**: 主程序 UI 和逻辑入口。
- **Qualcomm**: 核心高通协议实现 (Firehose Client, Sahara Client, Authentication Strategy)。
- **Common**: 通用工具类 (日志, 帮助类)。

## 编译指南

本项目使用 .NET 8.0 开发。

### 环境要求
- .NET 8.0 SDK

### 编译步骤

1. 打开解决方案 `lfEDL.sln` 或使用命令行进入目录。
2. 执行还原和编译：

```bash
dotnet restore
dotnet build
```

3. 运行程序:

```bash
dotnet run --project lfEDL.Avalonia
```

## 许可证

本项目代码基于 CC BY-NC-SA 4.0 许可证分发。
部分协议实现参考了开源社区的高通 EDL 项目。
