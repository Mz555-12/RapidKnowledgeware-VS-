
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Properties;
using RapidKnowledgeware.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace RapidKnowledgeware.ViewModels
{
    public class MainWindowModel
    {
        public MainModel MainModel { get; set; } = new MainModel();
        private CommandBase _closeMainWindowCommand;

        public CommandBase CloseMainWindowCommand
        {
            get
            {
                if (_closeMainWindowCommand == null)
                {
                    _closeMainWindowCommand = new CommandBase();
                    _closeMainWindowCommand.DoExecute = new Action<object>((o) =>
                    {
                        AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
                        (o as Window).Close();
                    });
                }
                return _closeMainWindowCommand;
            }
        }


        private CommandBase _openSpaceParametersViewCommand;
        private bool _isSpaceViewVisible = false;  // 状态标志
        public CommandBase OpenSpaceParametersViewCommand
        {
            get
            {
                if (_openSpaceParametersViewCommand == null)
                {
                    _openSpaceParametersViewCommand = new CommandBase();
                    _openSpaceParametersViewCommand.DoExecute = new Action<object>((o) =>
                    {

                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow == null)
                        {
                            return;
                        }

                        var overlay = mainWindow.FindName("OverlayContainer") as Grid;
                        var spaceView = mainWindow.FindName("SpaceView") as SpaceParametersView;

                        if (overlay == null || spaceView == null)
                        {
                            return;
                        }

                        if (!_isSpaceViewVisible)
                        {   
                            
                            WindowControls.Show_Title();

                            // 当前隐藏 → 显示（滑落）
                            Debug.WriteLine("[命令] 执行滑落动画");
                            SlidingView.SlideInFromLeft(spaceView, overlay);


                            _isSpaceViewVisible = true;
                        }
                        else
                        {
                            WindowControls.Hide_Title();

                            // 当前显示 → 隐藏（滑回）
                            Debug.WriteLine("[命令] 执行滑回动画");
                            SlidingView.SlideOutToRight(spaceView, overlay);
                            AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
                            _isSpaceViewVisible = false;
                        }
                    });
                }
                return _openSpaceParametersViewCommand;
            }
        }


        private CommandBase _test;

        public CommandBase Test
        {
            get
            {
                if (_test == null)
                {
                    _test = new CommandBase();
                    _test.DoExecute = new Action<object>(async (o) =>
                    {
                        // ========== 初始化服务 ==========
                        var rag = new RagService(
                            ollamaEndpoint: "http://localhost:11434",
                            embeddingModel: "bge-m3:567m",
                            chatModel: "qwen2.5:3b"
                        );

                        // 可选：设置默认生成参数
                        rag.DefaultLLMParameters.Temperature = 0.6f;
                        rag.DefaultLLMParameters.ContextSize = 4096;
                        rag.DefaultLLMParameters.SystemPrompt = "你是一个贴心、准确的女性健康知识助手。";

                        // ========== 测试 1：索引文档 ==========
                        Console.WriteLine("【测试 1】索引文档...");
                        try
                        {
                            // 请确保文件存在，支持多级分隔符
                            int chunkCount = await rag.IndexDocumentAsync(
                                filePath: @"E:\ColleageLife\Learn\聊天提示库\女生的四个阶段 - 副本.txt",
                                chunkSeparators: new[] { "###", "##" },
                                clearExisting: true
                            );
                            Console.WriteLine($"✅ 成功索引 {chunkCount} 个文档块。\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ 索引失败：{ex.Message}");
                            return;
                        }

                        // ========== 测试 2：非流式 RAG 查询 ==========
                        Console.WriteLine("【测试 2】非流式 RAG 查询...");
                        var response1 = await rag.QueryAsync(
                            query: "女生经期前一周应该怎么关心她？",
                            topK: 2,
                            minSimilarity: 0.5f
                        );

                        Console.WriteLine($"📌 检索到 {response1.RetrievedChunks.Count} 个相关块：");
                        foreach (var (chunk, sim) in response1.RetrievedChunks)
                        {
                            Console.WriteLine($"   - 相似度 {sim:F4} | 来源：{chunk.Metadata["source"]}");
                            Console.WriteLine($"     内容预览：{chunk.Content.Substring(0, Math.Min(40, chunk.Content.Length))}...");
                        }
                        Console.WriteLine($"\n💬 回答：\n{response1.Answer}\n");

                        // ========== 测试 3：流式 RAG 查询（支持中途取消） ==========
                        Console.WriteLine("【测试 3】流式 RAG 查询...");
                        var cts = new System.Threading.CancellationTokenSource();

                        // 模拟 8 秒后自动停止（演示取消功能）
                        //_ = Task.Run(async () =>
                        //{
                        //    await Task.Delay(8000);
                        //    if (!cts.IsCancellationRequested)
                        //    {
                        //        Console.WriteLine("\n⏹️ [模拟] 用户手动停止生成...");
                        //        cts.Cancel();
                        //    }
                        //});

                        var response2 = await rag.QueryStreamingToConsoleAsync(
                            query: "排卵期女生状态怎么样？",
                            topK: 3,
                            minSimilarity: 0.4f,
                            cancellationToken: cts.Token
                        );

                        Console.WriteLine("\n\n📋 本次流式回答使用的检索块：");
                        foreach (var (chunk, sim) in response2.RetrievedChunks)
                        {
                            Console.WriteLine($"   [{sim:F3}] {chunk.Content.Substring(0, Math.Min(30, chunk.Content.Length))}...");
                        }

                        // ========== 测试 4：清空索引后再索引另一个文件（演示复用） ==========
                        Console.WriteLine("\n【测试 4】清空索引，加载新文档...");
                        rag.ClearIndex();
                        Console.WriteLine($"当前索引块数：{rag.IndexedChunkCount}");

                        // 这里可以再次索引另一个文件
                        // await rag.IndexDocumentAsync("另一个文件.txt", new[] { "###" });

                        Console.WriteLine("\n✅ 所有测试执行完毕。");
                    });
                }
                return _test;
            }
        }
    }
}
