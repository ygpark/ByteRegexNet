using System;
using System.CodeDom;
using System.Collections.Generic;

namespace ByteRegexNet
{
    public class ByteRegex
    {
        private MyArrayList<ItemBase> target = new MyArrayList<ItemBase>();

        public static ByteRegex Compile(string pattern)
        {
            ByteRegex result = new ByteRegex();
            bool inSingleChar = false;
            bool inTimes = false;

            Items ?items = null;
            string strTimes = string.Empty;
            for (int i = 0; i < pattern.Length; i++)
            {
                //괄호 안과 밖 상태
                if (!inSingleChar && pattern[i] == '[')
                {
                    items = new Items();
                    inSingleChar = true;
                    continue;
                }

                if (inSingleChar && pattern[i] == ']')
                {
                    if (items != null)
                    {
                        result.target.Add(items);
                    }

                    items = null;
                    inSingleChar = false;
                    continue;
                }

                if (!inSingleChar && pattern[i] == '{')
                {
                    inTimes = true;
                    continue;
                }

                if (pattern[i] == '}')
                {
                    inTimes = false;

                    //{10} or {0,10}
                    string[] arr = strTimes.Split(',');
                    if (arr.Length < 1 || 2 < arr.Length)
                        throw new Exception("Comma must 0 or 1 in { }");

                    if (arr.Length == 1)
                    {
                        int minmax = int.Parse(arr[0]);
                        var lastItem = result.target[result.target.Count - 1];
                        lastItem.MinTimes(minmax);
                        lastItem.MaxTimes(minmax);
                    }
                    if (arr.Length == 2)
                    {
                        int min = int.Parse(arr[0]);
                        int max = int.Parse(arr[1]);
                        var lastItem = result.target[result.target.Count - 1];
                        lastItem.MinTimes(min);
                        lastItem.MaxTimes(max);
                    }

                    strTimes = String.Empty;

                    continue;
                }

                //====================================
                //
                //====================================


                //현재 상태 [  A B C D  ]
                //               -
                if (inSingleChar && pattern[i - 1] != '-')
                {
                    items?.Enable((byte)pattern[i]);
                }
                else if (inSingleChar && pattern[i - 1] == '-')
                {
                    //현재 상태 [  A - B ]
                    //               --
                    items?.Disable((byte)'-');
                    for (int j = (int)pattern[i - 2]; j <= pattern[i]; j++)
                    {
                        items?.Enable((byte)j);
                    }
                }
                else if (inTimes)
                {
                    //현재 상태 {  ______  } 또는 {  ___ , ___  }
                    //중괄호에 있는 문자형 숫자를 이어붙인 후 중괄호가 닫히면 숫자로 만든다.
                    strTimes += pattern[i];
                }
                else if (!inSingleChar && !inTimes)
                {
                    // 괄호 밖에 있는 상태

                    //특수문자
                    if (pattern[i] == '.')
                    {
                        Items any = new Items();
                        any.EnableAll();
                        result.target.Add(any);
                    }
                    //=====================================
                    // *와 + 기호 미지원에 대한 내용
                    // 문자열을 탐색할 때는 줄바꿈 등 끝이 있지만, byte[]를 탐색할 때는 끝을 알 수 없으므로 지원하지 않는다.
                    //=====================================
                    //else if (pattern[i] == '*')//0개 이상
                    //{
                    //    var lastItem = result.target.Last();
                    //    lastItem.MinTimes(0);
                    //    lastItem.MaxTimes(-1);//endless
                    //}
                    //else if (pattern[i] == '+')//0개 이상
                    //{
                    //    var lastItem = result.target.Last();
                    //    lastItem.MinTimes(1);
                    //    lastItem.MaxTimes(-1);//endless
                    //}
                    else
                    {
                        result.target.Add(new Item() { value = (byte)pattern[i] });
                    }
                }
            }

            return result;
        }

        public int Match(byte[] data)
        {
            int dataLength = data.Length;
            //길이 구하기
            if (dataLength < TargetLen())
                return -1;

            //전체 순회
            for (int i = 0; i < dataLength; i++)
            {
                int cursor = i;

                //정규식 순회
                for (int tidx = 0; tidx < target.Count; tidx++)
                {
                    int hit = 0;
                    ItemBase ib = target[tidx];
                    int min = ib.MinTimes();
                    int max = ib.MaxTimes();

                    //최대 횟수만큼 탐색
                    for (int j = 0; j < max; j++)
                    {
                        // index out of rage
                        if (dataLength - 1 < cursor)
                            break;

                        if (target[tidx].CompareTo(data[cursor]) == 0)
                        {
                            hit++;
                            cursor++;
                        }

                        // 최소 횟수를 만족하면서 다음 찾을 값이 같으면 max까지 탐색하지 않는다.
                        if (min <= hit && hit <= max && tidx + 1 < target.Count && target[tidx + 1].CompareTo(data[cursor]) == 0)
                        {
                            break;
                        }
                    }

                    if (hit < min || max < hit)
                    {
                        break;
                    }

                    if (tidx == target.Count - 1)
                        return i;
                }
            }

            return -1;
        }

        private int TargetLen()
        {
            int rst = 0;
            for (int i = 0; i < target.Count; i++)
            {
                rst += target[i].MinTimes();
            }
            return rst;
        }


        abstract class ItemBase : IComparable<byte>, IRange
        {
            private int minTimes = 1;
            private int maxTimes = 1;

            public int MinTimes() { return minTimes; }
            public void MinTimes(int value) { minTimes = value; }
            public int MaxTimes() { return maxTimes; }
            public void MaxTimes(int value) { maxTimes = value; }
            public virtual int CompareTo(byte other)
            {
                throw new NotImplementedException();
            }
        }

        interface IRange
        {
            int MinTimes();
            void MinTimes(int value);
            int MaxTimes();
            void MaxTimes(int value);
        }

        class Item : ItemBase
        {
            public byte value;

            public override int CompareTo(byte other)
            {
                return (value == other) ? 0 : 1;
            }
        }

        class Items : ItemBase
        {
            public byte[] values = new byte[256];

            public void Enable(byte value)
            {
                values[value] = 1;
            }

            public void Disable(byte value)
            {
                values[value] = 0;
            }

            public int Get(int index)
            {
                return values[index];
            }

            public void EnableAll()
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = 1;
                }
            }

            public override int CompareTo(byte other)
            {
                return (0 < values[other]) ? 0/*equal*/ : 1;
            }
        }

        public class MyArrayList<T>
        {
            public T[] arr;
            public int Capability = 2;
            public int Count = 0;

            public MyArrayList()
            {
                arr = new T[Capability];
            }
            public T this[int key]
            {
                get
                {
                    return arr[key];
                }
            }

            public void Add(T t)
            {
                arr[Count++] = t;
                if (Count >= Capability)
                {
                    T[] tmp = arr;
                    int oldCapability = Capability;
                    int newCapability = Capability * 2;
                    T[] tmp2 = new T[newCapability];
                    Array.Copy(tmp, tmp2, oldCapability);
                    arr = tmp2;

                    Capability = newCapability;
                }
            }
        }
    }
}