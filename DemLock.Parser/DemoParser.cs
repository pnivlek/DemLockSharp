﻿using DemLock.Parser.Models;

namespace DemLock.Parser;

/// <summary>
/// The main demo parser that will be used to set up the event listeners, and then send in a file to be
/// processed
/// </summary>
public class DemoParser
{
    public DemoEventSystem Events { get; }
    private FrameHandler _frameHandler;
    private MessageHandler _messageHandler;
    private DemoParserContext _context;
    private DemoParserConfig _config;

    public DemoParser()
    {
        _config = new DemoParserConfig();
        Events = new DemoEventSystem();
        _context = new DemoParserContext(_config);
        _context.Events = Events;
        _messageHandler = new MessageHandler(Events, _context);
        _frameHandler = new FrameHandler(Events, _messageHandler, _context);
    }

    public DemoParser(DemoParserConfig config)
    {
        _config = config;
        Events = new DemoEventSystem();
        _context = new DemoParserContext(_config);
        _context.Events = Events;
        _messageHandler = new MessageHandler(Events, _context);
        _frameHandler = new FrameHandler(Events, _messageHandler, _context);
    }

    /// <summary>
    /// Process a demo file, emitting events to any registered listeners when a derived event
    /// is calculated, which will contain data about the event (such as file info being parsed, 
    /// </summary>
    /// <param name="fileName"></param>
    public void ProcessDemo(string fileName)
    {
        // Make sure we clear our context to start fresh
        _context.ClearContext();
        using DemoFile demo = new DemoFile(fileName);
        DemoFrame frame;
        int i = 0;
        do
        {
            frame = demo.ReadFrame();
            _context.CurrentTick = frame.Tick;
            if (_config.LogReadFrames) Console.WriteLine($"[{i}::{frame.Tick}] {frame.Command}({(int)frame.Command})");
            _frameHandler.HandleFrame(frame);
            i++;
        } while (frame.Command != DemoFrameCommand.DEM_Stop);
    }

    public void DumpClassDefinitions(string fileName, string outputDirectory)
    {
        // Make sure we clear our context to start fresh
        _context.ClearContext();
        using DemoFile demo = new DemoFile(fileName);
        DemoFrame frame;
        int i = 0;
        do
        {
            frame = demo.ReadFrame();
            _context.CurrentTick = frame.Tick;
            if (frame.Command == DemoFrameCommand.DEM_SendTables || frame.Command == DemoFrameCommand.DEM_ClassInfo)
            {
                _frameHandler.HandleFrame(frame);
            }
            i++;
        } while (frame.Command != DemoFrameCommand.DEM_Stop);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        
            
        _context.DumpClassDefinitions(outputDirectory);
    }
}