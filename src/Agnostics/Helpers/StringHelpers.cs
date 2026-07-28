using System.IO;

namespace FEx.Agnostics.Helpers;

public static class StringHelpers
{
    public static long CountLinesMaybe(Stream stream)
    {
        var lineCount = 0L;

        var byteBuffer = new byte[1024 * 1024];
        const int bytesAtTheTime = 4;
        char? detectedEol = null;
        char? currentChar = null;

        int bytesRead;

        while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
        {
            var i = 0;

            for (; i <= bytesRead - bytesAtTheTime; i += bytesAtTheTime)
            {
                currentChar = (char)byteBuffer[i];

                if (detectedEol is not null)
                {
                    if (currentChar == detectedEol)
                        lineCount++;

                    currentChar = (char)byteBuffer[i + 1];

                    if (currentChar == detectedEol)
                        lineCount++;

                    currentChar = (char)byteBuffer[i + 2];

                    if (currentChar == detectedEol)
                        lineCount++;

                    currentChar = (char)byteBuffer[i + 3];

                    if (currentChar == detectedEol)
                        lineCount++;
                }
                else
                {
                    if (currentChar is '\n' or '\r')
                    {
                        detectedEol = currentChar;
                        lineCount++;
                    }

                    i -= bytesAtTheTime - 1;
                }
            }

            for (; i < bytesRead; i++)
            {
                currentChar = (char)byteBuffer[i];

                if (detectedEol is not null)
                {
                    if (currentChar == detectedEol)
                        lineCount++;
                }
                else
                {
                    if (currentChar is '\n' or '\r')
                    {
                        detectedEol = currentChar;
                        lineCount++;
                    }
                }
            }
        }

        if (currentChar != '\n'
            && currentChar != '\r'
            && currentChar is not null)
            lineCount++;

        return lineCount;
    }
}