using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

/*
 * 20180510 async write判断不适用？ Available since 4.5 ( .NET Framework )
 * 20180510 file.Flush()消耗比file.Write()高；放到临界区外面
     */

public class SimpleLogger
{
    private static bool m_enable = true;
    private static bool m_asyncwrite = true;
    private static bool m_abortWriteThread = false;
    private static string m_filePath = "";
    private static StreamWriter m_fileWriter = null;
    private static bool m_fileBufferMode = true;

    private static readonly object m_syncPrimitive = new object();
    private static Thread m_writeThread = null;
    private static StringBuilder m_logBuffer = new StringBuilder();
    private static int m_writeChunkSize = 1024; // Bytes

    // 全局开关
    public static bool Enable
    {
        get { return m_enable; }
        set { m_enable = value; }
    }

    // 异步写文件
    public static bool AsyncWrite
    {
        get { return m_asyncwrite; }
        set { m_asyncwrite = value; }
    }

    // 控制输出到文件时是否缓存
    public static bool FileBufferMode
    {
        get { return m_fileBufferMode; }
        set { m_fileBufferMode = value; }
    }

    // 日志量积累到一个ChunkSize(Bytes)才写
    public static int WriteChunkSize
    {
        get { return m_writeChunkSize; }
        set { m_writeChunkSize = value; }
    }

    // 输出一条Log
    public static bool Log(string line)
    {
        if (m_asyncwrite == true && !m_abortWriteThread) // 如果写线程已经停了，需要直接写文件
            __WriteToBuffer(line);
        else
            __WriteToFile(line);

        return true;
    }

    public static void FlushFile()
    {
        if (m_fileWriter != null)
        {
            Debug.Assert(m_asyncwrite == false);
            m_fileWriter.Flush();
        }
    }

    public static void SpawnWriteThread()
    {
        if (m_asyncwrite && m_writeThread == null)
        {
            Debug.Assert(m_abortWriteThread = false);
            m_filePath = Path.Combine(__logDir(), __logFileName());
            m_logBuffer.EnsureCapacity(2 * m_writeChunkSize);

            m_writeThread = new Thread(__WriteBufferToFile);
            m_writeThread.IsBackground = true;
            m_writeThread.Start();
        }
    }

    public static void DestroyWriteThread()
    {
        if (m_writeThread != null)
        {
            m_abortWriteThread = true;
            lock (m_syncPrimitive)
            {
                Monitor.Pulse(m_syncPrimitive);
            }

            m_writeThread.Join();
            m_writeThread = null;
        }
    }

    private static string __TimeStr()
    {
        //return "2015/04/28_11:04:54,984 ";
        return DateTime.Now.ToString("yyyy/MM/dd_HH:mm:ss,fff ");
    }

    private static void __WriteToBuffer(string line)
    {
        if (line.Length > 0)
        {
            lock (m_syncPrimitive)
            {
                m_logBuffer.Append(line);
                if (line[line.Length - 1] != '\n')
                    m_logBuffer.Append("\r\n");

                if (m_logBuffer.Length > m_writeChunkSize)
                    Monitor.Pulse(m_syncPrimitive);
            }
        }
    }

    private static void __WriteBufferToFile()
    {
        if (m_fileWriter == null)
        {
            try
            {
                m_fileWriter = new StreamWriter(m_filePath);
                m_fileWriter.WriteLine("[[TIME,LEVEL,LOGGER]]"); 
                m_fileWriter.Flush();
            }
            catch (Exception)
            {
                m_fileWriter = null;
            }
        }

        while (!m_abortWriteThread)
        {
            lock (m_syncPrimitive)
            {
                Monitor.Wait(m_syncPrimitive);

                if (m_fileWriter != null && m_logBuffer.Length > 0)
                {
                    try
                    {
                        m_fileWriter.Write(m_logBuffer);
                        m_logBuffer.Length = 0;
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            if (m_fileWriter != null)
            {
                try
                {
                    m_fileWriter.Flush();
                }
                catch (Exception)
                {
                }
            }
        }
    }

    private static void __WriteToFile(string line)
    {
        if (m_filePath.Length == 0)
        {
            m_filePath = Path.Combine(__logDir(), __logFileName());
            try
            {
                m_fileWriter = new StreamWriter(m_filePath);
                m_fileWriter.WriteLine("[[TIME,LEVEL,LOGGER]]"); 
                m_fileWriter.Flush();
            }
            catch (Exception)
            {
                m_fileWriter = null;
            }
        }

        if (m_fileWriter != null)
        {
            try
            {
                char lastChar = line[line.Length - 1];
                if (lastChar == '\n')
                    m_fileWriter.Write(line);
                else
                    m_fileWriter.WriteLine(line);

                if (!m_fileBufferMode)
                {
                    m_fileWriter.Flush();
                }
            }
            catch (Exception)
            {
            }
        }
    }

    private static string __logDir()
    {
        string dir = "logs";

        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (Exception)
        {
        }

        return dir;
    }

    private static string __logFileName()
    {
        DateTime now = DateTime.Now;
        string filename = string.Format(
            "{0:D4}{1:D2}{2:D2}_{3:D2}{4:D2}{5:D2}.txt",
            now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second
        );
        return filename;
    }
}
