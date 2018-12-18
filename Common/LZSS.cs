namespace SonicRetro.KensSharp
{
    using System;

    public static class LZSS
    {
        public struct NodeMeta
        {
            public int cost;
            public int next_node_index;
            public int previous_node_index;
            public int match_length;
            public int match_offset;
        };

        // What the fuck is wrong with this language? Why is four params okay, but when I need five I have to do this?
        public delegate void Action<T1, T2, T3, T4, T5>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5);

        public static NodeMeta[] FindMatches<T>(T[] data, int pos, int data_size, int max_match_length, int max_match_distance, Action<T[], int, int, int, NodeMeta[]> find_extra_matches, int literal_cost, Func<int, int, int> match_cost_callback)
        {
            NodeMeta[] node_meta_array = new NodeMeta[data_size + 1];

            node_meta_array[0].cost = 0;
            for (int i = 1; i < data_size + 1; ++i)
                node_meta_array[i].cost = int.MaxValue;

            for (int i = 0; i < data_size; ++i)
            {
				int max_read_ahead = Math.Min(max_match_length, data_size - i);
				int max_read_behind = max_match_distance > i ? 0 : i - max_match_distance;

                find_extra_matches(data, pos, data_size, i, node_meta_array);

                for (int j = i; j-- > max_read_behind;)
                {
                    for (int k = 0; k < max_read_ahead; ++k)
                    {
                        if (data[pos + i + k].Equals(data[pos + j + k]))
                        {
							int cost = match_cost_callback(i - j, k + 1);

                            if (cost != 0 && node_meta_array[i + k + 1].cost > node_meta_array[i].cost + cost)
                            {
                                node_meta_array[i + k + 1].cost = node_meta_array[i].cost + cost;
                                node_meta_array[i + k + 1].previous_node_index = i;
                                node_meta_array[i + k + 1].match_length = k + 1;
                                node_meta_array[i + k + 1].match_offset = j;
                            }
                        }
                        else
                            break;
                    }
                }

                if (node_meta_array[i + 1].cost >= node_meta_array[i].cost + literal_cost)
                {
                    node_meta_array[i + 1].cost = node_meta_array[i].cost + literal_cost;
                    node_meta_array[i + 1].previous_node_index = i;
                    node_meta_array[i + 1].match_length = 0;
                }
            }

            node_meta_array[0].previous_node_index = int.MaxValue;
            node_meta_array[data_size].next_node_index = int.MaxValue;
            for (int node_index = data_size; node_meta_array[node_index].previous_node_index != int.MaxValue; node_index = node_meta_array[node_index].previous_node_index)
                node_meta_array[node_meta_array[node_index].previous_node_index].next_node_index = node_index;

            return node_meta_array;
        }
    }
}
