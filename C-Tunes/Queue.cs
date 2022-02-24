using DSharpPlus.Lavalink;

namespace C_Tunes
{
    public class TrackQueue
    {
        private List<LavalinkTrack> tracks = new List<LavalinkTrack>();
        private LavalinkTrack? currentTrack = null;
        private bool testVar = false;

        public TrackQueue(bool test)
        {
            Console.WriteLine("New Queue");
            testVar = test;
        }

        public bool IsEmpty()
        {
            Console.WriteLine(tracks);
            Console.WriteLine(currentTrack);
            return tracks.Count == 0 && currentTrack == null;
        }

        public bool QueueExists()
        {
            return tracks.Count > 0;
        }

        public LavalinkTrack GetNext()
        {
            currentTrack = tracks[0];
            tracks.RemoveAt(0);
            return currentTrack;
        }

        public LavalinkTrack GetSafeNext()
        {
            return currentTrack;
        }

        public void AddTrack(LavalinkTrack track)
        {
            Console.WriteLine("Bfore adding song, the queue had a length of " + tracks.Count + " and currenTrack is " + (currentTrack != null) + ". TestVar is " + (testVar));
            testVar = false;
            tracks.Add(track);
            Console.WriteLine("Added song, queue length is now " + tracks.Count);
        }

        public string GetQueue()
        {
            if(currentTrack != null)
            {
                string[] queue = new string[tracks.Count + 1];

                Console.WriteLine(queue.Length);

                queue[0] = currentTrack.Title;

                for (int i = 1; i < queue.Length; i++)
                {
                    queue[i] = (i + 2) + ". " + tracks[i].Title;
                }

                Console.WriteLine(queue[0]);
                return string.Join("\n", queue);
            } else
            {
                return "No songs in queue";
            }
            
        }
    }
}
