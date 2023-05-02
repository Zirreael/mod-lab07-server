using System;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace serv_client
{
    class Program
    {
        static void Main()
        {
            Stopwatch sw = new Stopwatch();
            double T;
            double clientIntens = 30;
            double serverIntens = 3;
            double P, P_real, Q, Q_real, P0, P0_real, A, A_real, k, k_real, R, R_real, l, nu;
            int poolCount = 5;
            int appCount = 100;
            int number = 9;
            Server server = new Server(poolCount, serverIntens);
            Client client = new Client(server);
            sw.Start();
            for(int id = 1; id <= appCount; id++)
            {
                client.send(id);
                Thread.Sleep(50);
            }
            sw.Stop();
            long time = sw.ElapsedMilliseconds;
            T = (double)time/1000;
            Console.WriteLine("Всего заявок: {0}", server.requestCount);
            Console.WriteLine("Обработано заявок: {0}", server.processedCount);
            Console.WriteLine("Отклонено заявок: {0}", server.rejectedCount);
            l = server.requestCount / T;
            nu = server.processedCount / (T * poolCount);
            R = clientIntens / serverIntens;
            P0 = calcP0(poolCount, R);
            P = calcP(poolCount, R, P0);
            Q = 1 - P;
            A = clientIntens * Q;
            k = A / serverIntens;
            R_real = l / nu;
            P0_real = calcP0(poolCount, R_real);
            P_real = calcP(poolCount, R_real, P0_real);
            Q_real = 1 - P_real;
            A_real = l * Q_real;
            k_real = A_real / nu;

            string name = "results.txt";
            StreamWriter swr = new StreamWriter(name, true);
            string text = "\nЗапуск №" + number + "\nВремя работы: " + T + " с";
            text += "\n\nПараметры:\n" + "Интенсивность потока заявок: " + clientIntens + "\nИнтенсивность потока обслуживания: " + serverIntens + "\nКоличество потоков:" + poolCount;
            text += "\n\nДанные сервера:\n" + "\nВсего заявок: " + server.requestCount + "\nОбработано заявок: " + server.processedCount + "\nОтклонено заявок: " + server.rejectedCount;
            text += "\n\nТеоритические данные:\n" + "\nВероятность простоя системы: " + P0;
            text += "\nВероятность отказа системы: " + P + "\nОтносительная пропускная способность: " + Q;
            text += "\nАбсолютная пропускная способность: " + A + "\nСреднее число занятых каналов: " + k;
            text += "\n\nРеальные данные:\n" + "\nВероятность простоя системы: " + P0_real;
            text += "\nВероятность отказа системы: " + P_real + "\nОтносительная пропускная способность: " + Q_real;
            text += "\nАбсолютная пропускная способность: " + A_real + "\nСреднее число занятых каналов: " + k_real;
            text += "\n----------------------------------------------------------";
            swr.Write(text);
            swr.Close();

            static double calcP0(int poolCount, double R)
            {
                double p0 = 0;
                for(int i = 0; i <= poolCount; i++)
                {
                    p0 += Math.Pow(R, i) / fact(i);
                }
                return Math.Pow(p0, -1);
            }
            static double calcP(int poolCount, double R, double P0)
            {
                double p = (Math.Pow(R, poolCount) / fact(poolCount)) * P0;
                return p;
            }
            static double fact(double n)
            {
                double res = 1;
                for(int i = 1; i <= n; i++)
                {
                    res *= i;
                }
                return res;
            }
            
        }
    }
    struct PoolRecord
    {
        public Thread thread;
        public bool in_use;
    }
    class Server
    {
        private PoolRecord[] pool;
        private object threadLock = new object();
        public int requestCount = 0;
        public int processedCount = 0;
        public int rejectedCount = 0;
        public double intens;
        public Server(int poolCount, double intens)
        {
            pool = new PoolRecord[poolCount];
            this.intens = intens;
        }
        public void proc(object sender, procEventArgs e)
        {
            lock (threadLock)
            {
                Console.WriteLine("Заявка с номером: {0} поступила", e.id);
                requestCount++;
                for(int i = 0; i < pool.Length; i++)
                {
                    if (!pool[i].in_use)
                    {
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        pool[i].thread.Start(e.id);
                        processedCount++;
                        Console.WriteLine("Заявка с номером: {0} принята", e.id);
                        return;
                    }
                }
                rejectedCount++;
                Console.WriteLine("Заявка с номером: {0} отклонена", e.id);
            }
        }
        public void Answer(object arg)
        {
            int id = (int)arg;
            Console.WriteLine("Обработка заявки: {0}", id);
            Thread.Sleep(TimeSpan.FromSeconds(1 / intens));
            for(int i = 0; i < pool.Length; i++)
            {
                if (pool[i].thread == Thread.CurrentThread)
                    pool[i].in_use = false;
            }
        }
    }
    class Client
    {
        private Server server;
        public Client(Server server)
        {
            this.server = server;
            this.request += server.proc;
        }
        public void send(int id)
        {
            procEventArgs args = new procEventArgs();
            args.id = id;
            OnProc(args);
        }
        protected virtual void OnProc(procEventArgs e)
        {
            EventHandler<procEventArgs> handler = request;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<procEventArgs> request;
    }
    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
    }
}
