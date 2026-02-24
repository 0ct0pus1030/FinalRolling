using System;
using System.IO;
using FGLogic.Input;
using UnityEngine;

namespace FGLogic.Core
{
    public class GameInputRecorder
    {
        private FileStream _fs;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private string _filePath;
        private int _totalFrames = 0;

        private const uint MAGIC = 0x46475231; // "FGR1"
        private const int HEADER_SIZE = 9;

        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }
        public int CurrentFrame { get; private set; }
        public int TotalFrames => _totalFrames;

        public void StartRecord(string fileName)
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));

            _fs = new FileStream(_filePath, FileMode.Create);
            _writer = new BinaryWriter(_fs);

            _writer.Write(MAGIC);
            _writer.Write((byte)1); // 版本
            _writer.Write(0); // 总帧数占位

            CurrentFrame = 0;
            IsRecording = true;
        }

        public void WriteFrame(FrameInput input)
        {
            if (!IsRecording) return;
            _writer.Write(input.Serialize());
            CurrentFrame++;
        }

        public void StopRecord()
        {
            if (!IsRecording) return;

            _fs.Seek(5, SeekOrigin.Begin);
            _writer.Write(CurrentFrame);
            _writer.Close();
            _fs = null;
            IsRecording = false;
        }

        public void StartPlayback(string fileName)
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(_filePath)) throw new FileNotFoundException(_filePath);

            _fs = new FileStream(_filePath, FileMode.Open);
            _reader = new BinaryReader(_fs);

            uint magic = _reader.ReadUInt32();
            byte ver = _reader.ReadByte();
            _totalFrames = _reader.ReadInt32();

            if (magic != MAGIC) throw new Exception("文件格式错误");

            CurrentFrame = 0;
            IsPlaying = true;
        }

        public bool ReadFrame(int playerId, out FrameInput input)
        {
            input = FrameInput.CreateEmpty(playerId, CurrentFrame);

            if (!IsPlaying || _fs.Position >= _fs.Length)
            {
                IsPlaying = false;
                return false;
            }

            byte[] data = _reader.ReadBytes(5);
            input = FrameInput.Deserialize(data, playerId);
            CurrentFrame++;
            return true;
        }

        public void StopPlayback()
        {
            _reader?.Close();
            _fs = null;
            IsPlaying = false;
        }
    }
}