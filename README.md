# mini_fy — Windows 截图翻译工具

轻量级 Windows 截图翻译小程序。按下快捷键框选屏幕区域，自动识别英文文字并通过大模型翻译为中文。

## 技术栈

- WPF (.NET 8)
- Windows.Media.Ocr（系统内置 OCR）
- 百度大模型文本翻译 API

## 运行环境

- Windows 10/11
- .NET 8 Desktop Runtime
- 系统须安装英文 OCR 语言包（设置 → 语言 → 添加语言 → English）

## 快速开始

### 1. 安装 .NET 8 SDK/Runtime

从 https://dotnet.microsoft.com/download/dotnet/8.0 下载安装。

### 2. 安装英文 OCR 语言包

Windows 设置 → 时间和语言 → 语言和区域 → 添加语言 → English (United States)

### 3. 配置百度翻译 API

1. 注册百度翻译开放平台：https://fanyi-api.baidu.com/
2. 开通"大模型文本翻译"服务
3. 在【管理控制台】→【API Key管理】创建 API Key
4. 获取 APPID 和 API Key

### 4. 运行程序

```bash
cd src/mini_fy.App
dotnet run
```

首次运行会在程序目录生成 `settings.json`，通过托盘菜单"设置"配置 API 密钥。

## 使用方式

1. 程序启动后常驻系统托盘
2. 按 `Ctrl + Alt + Q` 进入截图模式
3. 鼠标拖拽框选需要翻译的区域
4. 松开鼠标后自动识别并翻译
5. 翻译结果以悬浮窗展示

## 打包发布

```bash
cd src/mini_fy.App
dotnet publish -c Release -o publish /p:PublishSingleFile=true /p:SelfContained=false
```

## 项目结构

```
mini_fy/
├── src/mini_fy.App/      # WPF 主项目
│   ├── Services/         # 核心服务（Hotkey, Screenshot, OCR, Translate, Overlay, Tray, Settings）
│   ├── Models/           # 数据模型
│   ├── Views/            # WPF 窗口
│   └── Helpers/          # Win32 P/Invoke, 日志
├── src/mini_fy.Tests/    # 单元测试
├── config/               # 配置模板
└── docs/                 # 设计文档
```

## 注意事项

- 游戏中使用时，建议"无边框窗口"或"窗口化全屏"模式以获得最佳叠加体验
- API 密钥仅存储在本地 `settings.json`，请勿将该文件分享给他人
- 程序不上传截图，仅发送 OCR 提取的文本进行翻译
