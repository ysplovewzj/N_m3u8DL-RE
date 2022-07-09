﻿using N_m3u8DL_RE.Parser.Config;
using N_m3u8DL_RE.Common.Entity;
using N_m3u8DL_RE.Common.Enum;
using N_m3u8DL_RE.Parser.Util;
using N_m3u8DL_RE.Parser;
using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;
using N_m3u8DL_RE.Common.Resource;
using N_m3u8DL_RE.Common.Log;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using N_m3u8DL_RE.Subtitle;
using System.Collections.Concurrent;
using N_m3u8DL_RE.Common.Util;
using N_m3u8DL_RE.Processor;

namespace N_m3u8DL_RE
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            string loc = "en-US";
            string currLoc = Thread.CurrentThread.CurrentUICulture.Name;
            if (currLoc == "zh-TW" || currLoc == "zh-HK" || currLoc == "zh-MO") loc = "zh-TW";
            else if (currLoc == "zh-CN" || currLoc == "zh-SG") loc = "zh-CN";
            //设置语言
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(loc);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(loc);
            //Logger.LogLevel = LogLevel.DEBUG;

            try
            {
                var config = new ParserConfig();
                //demo1
                config.ContentProcessors.Insert(0, new DemoProcessor());
                //demo2
                config.KeyProcessors.Insert(0, new DemoProcessor2());

                var url = string.Empty;
                //url = "http://livesim.dashif.org/livesim/mup_300/tsbd_500/testpic_2s/Manifest.mpd";
                url = "http://playertest.longtailvideo.com/adaptive/oceans_aes/oceans_aes.m3u8";
                //url = "https://bitmovin-a.akamaihd.net/content/art-of-motion_drm/mpds/11331.mpd";



                if (string.IsNullOrEmpty(url))
                {
                    url = AnsiConsole.Ask<string>("请输入 [green]URL[/]: ");
                }

                //流提取器配置
                var extractor = new StreamExtractor(config);
                extractor.LoadSourceFromUrl(url);

                //解析流信息
                var streams = await extractor.ExtractStreamsAsync();

                //全部媒体
                var lists = streams.OrderByDescending(p => p.Bandwidth);
                //基本流
                var basicStreams = lists.Where(x => x.MediaType == null);
                //可选音频轨道
                var audios = lists.Where(x => x.MediaType == MediaType.AUDIO);
                //可选字幕轨道
                var subs = lists.Where(x => x.MediaType == MediaType.SUBTITLES);

                Logger.Warn(ResString.writeJson);
                await File.WriteAllTextAsync("meta.json", GlobalUtil.ConvertToJson(lists), Encoding.UTF8);

                Logger.Info(ResString.streamsInfo, lists.Count(), basicStreams.Count(), audios.Count(), subs.Count());

                foreach (var item in lists)
                {
                    Logger.InfoMarkUp(item.ToString());
                }

                //展示交互式选择框
                var selectedStreams = PromptUtil.SelectStreams(lists);
                //一个以上的话，需要手动重新加载playlist
                if (lists.Count() > 1)
                    await extractor.FetchPlayListAsync(selectedStreams);
                Logger.Warn(ResString.writeJson);
                await File.WriteAllTextAsync("meta_selected.json", GlobalUtil.ConvertToJson(selectedStreams), Encoding.UTF8);
                Logger.Info(ResString.selectedStream);
                foreach (var item in selectedStreams)
                {
                    Logger.InfoMarkUp(item.ToString());
                }

                Logger.Info("按任意键继续");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            //Console.ReadKey();
        }

        static void Print(object o)
        {
            Console.WriteLine(GlobalUtil.ConvertToJson(o));
        }
    }
}