// See https://aka.ms/new-console-template for more information
using ByteRegexNet;

byte[] data = new byte[100];

for (int i = 0; i < data.Length; i++)
{
    data[i] = (byte)i;
}

data[0] = (byte)'[';
data[1] = (byte)']';
data[10] = (byte)'a';
data[11] = (byte)'b';
data[12] = (byte)'C';
data[13] = (byte)'D';
data[14] = (byte)'c';
data[15] = (byte)'d';

data[18] = (byte)'e';

data[94] = (byte)'{';
data[95] = (byte)'[';
data[96] = (byte)'e';
data[97] = (byte)'\x90';
data[98] = (byte)'f';
data[99] = (byte)'f';

ByteRegex regex = ByteRegex.Compile("a.CD");
Console.WriteLine(regex.Match(data));

regex = ByteRegex.Compile("e.{60,90}[{]");
Console.WriteLine(regex.Match(data));

regex = ByteRegex.Compile("e[\x90]ff");
Console.WriteLine(regex.Match(data));

regex = ByteRegex.Compile("a[a-zA-Z]{4}");
Console.WriteLine(regex.Match(data));

