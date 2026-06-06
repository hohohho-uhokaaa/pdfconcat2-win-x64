//
// pdfconcat2  concatinate 2 pdf files in 1 (append) or all pdfs in 1 pdf file with page appending order
// coded by google gemini
// for all person who hates such a f*cking damn work by hand also em NA-KA-MA!!
// 06/03/2026 Ver. 0.2
//
// directory tree and pdf file store
//
// <page1 directory>     <page2 directory>
// |-00000001.pdf        |-00000001.pdf
// |-00000002.pdf        |-00000002.pdf
// |-00000003.pdf        |-00000003.pdf
// |                     |
// assumption: each pdf file contains only one page.  more than 2 pages will change its appending order between append or all. 
//
//
// append mode: append  outline also added ist page filename 2nd page some word
// page1/00000001.pdf + page2/00000001.pdf -> output/00000001.pdf
// page1/00000002.pdf + page2/00000002.pdf -> output/00000002.pdf
// page1/00000003.pdf + page2/00000003.pdf -> output/00000003.pdf
//
// append mode : all outline added 1st page filename 2nd page no outline
// page1/00000001.pdf + page2/00000001.pdf + page1/00000002.pdf + page2/00000002.pdf + page1/00000003.pdf + page2/00000003.pdf -> output/alllin1.pdf
//
// where 2 pages in pdf files in append mode
// page1/00000001.pdf of page 1/2 + page2/00000001.pdf of page 1/2 + page1/00000001.pdf of page 2/2 + page2/00000001.pdf of page 2/2
// generates allin1.pdf in this page order
//
// where 2 pages in pdf files in all mode
// page1/00000001.pdf of page 1/2 + page1/00000001.pdf of page 2/2 + page2/00000001.pdf of page 1/2 + page2/00000001.pdf of page 2/2
// generates allin1.pdf in this page order
//
// cli
// $ pdfconcat2 <page1-dir> <page2-dir> append|all  or debug run with launch.json
// place page1-dir page2-dir on your $home outpu dir will gen after cli exec
// so you can type your cli command line like $ pdfconcat2 page1 page2 append|all
// no need full path of dir
//
// on .csjpro for free no charge for you
// <ItemGroup>
//   <PackageReference Include="PdfSharpCore" Version="1.3.67" />
//   <PackageReference Include="SixLabors.ImageSharp" Version="2.1.11" />
// </ItemGroup>
//
// or
//
// add package first
// on bash
// dotnet add package PdfSharpCore --version 1.3.67
// dotnet add package SixLabors.ImageSharp --version 2.1.11
// on PowerShell
// Install-Package PdfSharpCore -Version 1.3.67
// Install-Package SixLabors.ImageSharp --version 2.1.11
//

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace pdfconcat2;

class Program
{
    static void Main(string[] args)
    {
        // デバッグ情報：受け取った引数を表示
        Console.WriteLine(string.Format(
            pdfconcat2.Properties.Messages.DebugArgCount,
            args.Length,
            string.Join(", ", args)
        ));

        // 引数の個数チェック：3個未満の場合は使用方法を表示して終了
        if (args.Length < 3)
        {
            ShowUsage();
            return;
        }

        // コマンドライン引数から入力ディレクトリとモードを取得
        string dir1 = args[0];
        string dir2 = args[1];
        string mode = args[2].ToLower();

        // モード引数の検証：appendまたはallのみが有効
        if (mode != "append" && mode != "all")
        {
            Console.WriteLine(pdfconcat2.Properties.Messages.ErrorMode);
            return;
        }

        // 入力ディレクトリの存在確認
        if (!Directory.Exists(dir1) || !Directory.Exists(dir2))
        {
            Console.WriteLine(pdfconcat2.Properties.Messages.DirectoryNotFound);
            return;
        }

        // 出力ディレクトリのパスを設定（ホームディレクトリ内の output フォルダ）
        string projectRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string outputDir = Path.Combine(projectRoot, "output");
        string finalAllInOnePath = Path.Combine(outputDir, "allin1.pdf");

        // 出力ディレクトリが存在しない場合は作成
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        try
        {
            // =================================================================
            // ステップ 1: 個別ペアの結合（ここで要件通りのしおりを各PDFに埋め込む）
            // =================================================================
            Console.WriteLine(string.Format(
                pdfconcat2.Properties.Messages.Step1Start,
                mode
            ));

            // dir1 内の 8 桁のファイル名を持つPDFのみを取得（例：00000001.pdf）
            string[] filesInDir1 = Directory.GetFiles(dir1, "*.pdf")
                .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^\d{8}\.pdf$", RegexOptions.IgnoreCase))
                .ToArray();

            int processedCount = 0;

            // 各ファイルペアを処理
            foreach (string file1Path in filesInDir1)
            {
                // dir2 内に対応するファイルが存在するか確認
                string fileName = Path.GetFileName(file1Path);
                string file2Path = Path.Combine(dir2, fileName);

                if (!File.Exists(file2Path))
                {
                    Console.WriteLine(string.Format(
                        pdfconcat2.Properties.Messages.FileNotFound,
                        fileName
                    ));
                    continue;
                }

                string singleOutputPath = Path.Combine(outputDir, fileName);
                Console.WriteLine(string.Format(
                    pdfconcat2.Properties.Messages.ProcessingFile,
                    fileName
                ));

                // ファイル名（拡張子なし）をしおりのタイトルとして使用
                string bookmarkTitle = Path.GetFileNameWithoutExtension(fileName);

                // 選択されたモードに応じて個別ファイルを生成
                if (mode == "append")
                {
                    // append モード：file1 を先頭に、file2 全体を後ろに追加
                    CreateSingleAppendPdf(file1Path, file2Path, singleOutputPath, bookmarkTitle);
                }
                else if (mode == "all")
                {
                    // all モード：file1 と file2 のページを交互に結合
                    CreateSingleInterleavePdf(file1Path, file2Path, singleOutputPath, bookmarkTitle);
                }

                processedCount++;
            }

            if (processedCount == 0)
            {
                Console.WriteLine(pdfconcat2.Properties.Messages.NoPairsFound);
                return;
            }

            // =================================================================
            // ステップ 2: 個別PDFを統合（個別ファイルが持つしおり構造を維持してマージ）
            // =================================================================
            Console.WriteLine($"\n{pdfconcat2.Properties.Messages.Step2Start}");

            // ステップ1で生成された個別ファイルを、ファイル名でソートして取得
            string[] generatedOutputs = Directory.GetFiles(outputDir, "*.pdf")
                .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^\d{8}\.pdf$", RegexOptions.IgnoreCase))
                .OrderBy(f => Path.GetFileName(f))
                .ToArray();

            // 最終的な統合PDFドキュメントを作成
            using (PdfDocument finalDocument = new PdfDocument())
            {
                // 生成された各PDFファイルを最終ドキュメントに追加
                foreach (string outputPath in generatedOutputs)
                {
                    Console.WriteLine(string.Format(
                        pdfconcat2.Properties.Messages.Step2Adding,
                        Path.GetFileName(outputPath)
                    ));

                    // 各PDFファイルを読み込んで、ページとしおり構造をコピー
                    using (PdfDocument inputPart = PdfReader.Open(outputPath, PdfDocumentOpenMode.Import))
                    {
                        // 現在の統合ドキュメントのページ数を取得（ページのオフセットとして使用）
                        int basePageIndex = finalDocument.PageCount;

                        // すべてのページを最終ドキュメントに追加
                        for (int i = 0; i < inputPart.PageCount; i++)
                        {
                            finalDocument.AddPage(inputPart.Pages[i]);
                        }

                        // 各PDFファイルが持つしおり情報を、正しいページ位置にマッピングして最終ドキュメントに追加
                        foreach (PdfOutline outline in inputPart.Outlines)
                        {
                            // 元のドキュメント内でしおりが参照していたページのインデックスを特定
                            int originalTargetIdx = inputPart.Pages.Cast<PdfPage>().ToList().IndexOf(outline.DestinationPage);
                            if (originalTargetIdx >= 0)
                            {
                                // しおりのページ参照を、統合後のドキュメント内の正しい位置に調整して追加
                                finalDocument.Outlines.Add(outline.Title, finalDocument.Pages[basePageIndex + originalTargetIdx]);
                            }
                        }
                    }
                }

                // 最終ドキュメントをファイルに保存
                finalDocument.Save(finalAllInOnePath);
            }

            // 完了メッセージを表示
            Console.WriteLine($"\n{pdfconcat2.Properties.Messages.CompleteSuccess}");
            Console.WriteLine(string.Format(
                pdfconcat2.Properties.Messages.OutputDirectory,
                outputDir,
                processedCount
            ));
            Console.WriteLine(string.Format(
                pdfconcat2.Properties.Messages.FinalFile,
                finalAllInOnePath
            ));
        }
        catch (Exception ex)
        {
            // エラーが発生した場合はエラーメッセージを表示
            Console.WriteLine(string.Format(
                pdfconcat2.Properties.Messages.ErrorOccurred,
                ex.Message
            ));
        }
    }

    /// <summary>
    /// 使用方法をコンソールに表示
    /// </summary>
    static void ShowUsage()
    {
        Console.WriteLine(pdfconcat2.Properties.Messages.UsageHeader);
        Console.WriteLine(pdfconcat2.Properties.Messages.UsageFormat);
        Console.WriteLine(pdfconcat2.Properties.Messages.UsageExample);
    }

    /// <summary>
    /// 【Step1用】appendモード：file1の全ページの後ろに file2 の全ページを追加し、しおりを埋め込む
    /// 出力ファイル形式：file1のページ + file2のページ
    /// しおり：file1の先頭ページ、file2の先頭ページ
    /// </summary>
    static void CreateSingleAppendPdf(string file1, string file2, string outputPath, string bookmarkTitle)
    {
        using (PdfDocument outputDocument = new PdfDocument())
        {
            using (PdfDocument inputDocument1 = PdfReader.Open(file1, PdfDocumentOpenMode.Import))
            using (PdfDocument inputDocument2 = PdfReader.Open(file2, PdfDocumentOpenMode.Import))
            {
                // file1 のすべてのページを追加
                for (int i = 0; i < inputDocument1.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument1.Pages[i]);
                }

                // file2 のすべてのページを後ろに追加
                for (int i = 0; i < inputDocument2.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument2.Pages[i]);
                }

                // file1 の先頭ページ（ページ0）にしおりを設定
                if (outputDocument.PageCount >= 1)
                {
                    outputDocument.Outlines.Add(bookmarkTitle, outputDocument.Pages[0]);
                }

                // file2 の先頭ページ（inputDocument1.PageCount ページ目）にしおりを設定
                if (outputDocument.PageCount >= 2)
                {
                    outputDocument.Outlines.Add("資料", outputDocument.Pages[inputDocument1.PageCount]);
                }
            }
            outputDocument.Save(outputPath);
        }
    }

    /// <summary>
    /// 【Step1用】allモード：file1 と file2 のページを交互に結合し、file1の先頭ページのみにしおりを追加
    /// 出力ファイル形式：file1のページ0 + file2のページ0 + file1のページ1 + file2のページ1 + ...
    /// しおり：file1の先頭ページのみ
    /// </summary>
    static void CreateSingleInterleavePdf(string file1, string file2, string outputPath, string bookmarkTitle)
    {
        using (PdfDocument outputDocument = new PdfDocument())
        {
            using (PdfDocument inputDocument1 = PdfReader.Open(file1, PdfDocumentOpenMode.Import))
            using (PdfDocument inputDocument2 = PdfReader.Open(file2, PdfDocumentOpenMode.Import))
            {
                // 各ドキュメントのページ数を取得
                int p1Count = inputDocument1.PageCount;
                int p2Count = inputDocument2.PageCount;
                int maxPages = Math.Max(p1Count, p2Count);

                // file1 と file2 のページを交互に追加
                for (int i = 0; i < maxPages; i++)
                {
                    // file1 のページ i が存在すれば追加
                    if (i < p1Count)
                    {
                        outputDocument.AddPage(inputDocument1.Pages[i]);
                    }

                    // file2 のページ i が存在すれば追加
                    if (i < p2Count)
                    {
                        outputDocument.AddPage(inputDocument2.Pages[i]);
                    }
                }

                // file1 の先頭ページ（ページ0）にのみしおりを設定
                if (outputDocument.PageCount >= 1)
                {
                    outputDocument.Outlines.Add(bookmarkTitle, outputDocument.Pages[0]);
                }
            }
            outputDocument.Save(outputPath);
        }
    }
}
