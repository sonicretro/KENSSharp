namespace SonicRetro.KensSharp
{
    using System;

    public static class LZSS
    {
        public struct NodeMeta
        {
            public long cost;
            public long next_node_index;
            public long previous_node_index;
            public long match_length;
            public long match_offset;
        };

        public static NodeMeta[] FindMatches(byte[] data, long data_size, long max_match_length, long max_match_distance, Action<byte[], long, long, NodeMeta[]> find_extra_matches, long literal_cost, Func<long, long, long> match_cost_callback)
        {
            NodeMeta[] node_meta_array = new NodeMeta[data_size + 1];

            node_meta_array[0].cost = 0;
            for (long i = 1; i < data_size + 1; ++i)
                node_meta_array[i].cost = long.MaxValue;

            for (long i = 0; i < data_size; ++i)
            {
                long max_read_ahead = Math.Min(max_match_length, data_size - i);
                long max_read_behind = max_match_distance > i ? 0 : i - max_match_distance;

                find_extra_matches(data, data_size, i, node_meta_array);

                for (long j = i; j-- > max_read_behind;)
                {
                    for (long k = 0; k < max_read_ahead; ++k)
                    {
                        if (data[i + k] == data[j + k])
                        {
                            long cost = match_cost_callback(i - j, k + 1);

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

            node_meta_array[0].previous_node_index = long.MaxValue;
            node_meta_array[data_size].next_node_index = long.MaxValue;
            for (long node_index = data_size; node_meta_array[node_index].previous_node_index != long.MaxValue; node_index = node_meta_array[node_index].previous_node_index)
                node_meta_array[node_meta_array[node_index].previous_node_index].next_node_index = node_index;

            return node_meta_array;
        }
    }
}
