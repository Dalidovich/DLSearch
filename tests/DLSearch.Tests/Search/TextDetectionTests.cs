using System.Text;
using DLSearch.Search;

namespace DLSearch.Tests.Search;

public class TextDetectionTests
{
    [Fact]
    public void Detect_EmptyHead_ReturnsText()
    {
        Assert.Equal(TextKind.Text, TextDetection.Detect([]));
    }

    [Fact]
    public void Detect_PlainAscii_ReturnsText()
    {
        Assert.Equal(TextKind.Text, TextDetection.Detect("plain text"u8));
    }

    [Fact]
    public void Detect_Utf8Bom_ReturnsTextEvenWithZeroBytes()
    {
        byte[] head = [0xEF, 0xBB, 0xBF, 0x00, 0x00];

        Assert.Equal(TextKind.Text, TextDetection.Detect(head));
    }

    [Fact]
    public void Detect_Utf16LittleEndianBom_ReturnsLittleEndian()
    {
        Assert.Equal(TextKind.Utf16LittleEndian, TextDetection.Detect([0xFF, 0xFE, 0x41, 0x00]));
    }

    [Fact]
    public void Detect_Utf16BigEndianBom_ReturnsBigEndian()
    {
        Assert.Equal(TextKind.Utf16BigEndian, TextDetection.Detect([0xFE, 0xFF, 0x00, 0x41]));
    }

    [Fact]
    public void Detect_ZeroByteWithoutBom_ReturnsBinary()
    {
        byte[] head = [0x41, 0x42, 0x00, 0x43];

        Assert.Equal(TextKind.Binary, TextDetection.Detect(head));
    }

    [Fact]
    public void Detect_ZeroByteAtInspectionBoundary_ReturnsBinary()
    {
        byte[] head = new byte[9000];
        Array.Fill(head, (byte)'a');
        head[8191] = 0;

        Assert.Equal(TextKind.Binary, TextDetection.Detect(head));
    }

    [Fact]
    public void Detect_ZeroByteBeyondInspectedPrefix_ReturnsText()
    {
        byte[] head = new byte[9000];
        Array.Fill(head, (byte)'a');
        head[8192] = 0;

        Assert.Equal(TextKind.Text, TextDetection.Detect(head));
    }

    [Fact]
    public void Detect_TruncatedBom_IsNotTreatedAsBom()
    {
        Assert.Equal(TextKind.Text, TextDetection.Detect([0xEF]));
    }

    [Theory]
    [InlineData(TextKind.Utf16LittleEndian)]
    [InlineData(TextKind.Text)]
    public void EncodingFor_NonBigEndianKinds_ReturnLittleEndianUnicode(TextKind kind)
    {
        Assert.Equal(Encoding.Unicode, TextDetection.EncodingFor(kind));
    }

    [Fact]
    public void EncodingFor_BigEndianKind_ReturnsBigEndianUnicode()
    {
        Assert.Equal(Encoding.BigEndianUnicode, TextDetection.EncodingFor(TextKind.Utf16BigEndian));
    }
}
