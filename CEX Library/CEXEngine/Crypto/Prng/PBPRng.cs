﻿#region Directives
using System;
using VTDev.Libraries.CEXEngine.Crypto.Digest;
using VTDev.Libraries.CEXEngine.Crypto.Enumeration;
using VTDev.Libraries.CEXEngine.Crypto.Generator;
using VTDev.Libraries.CEXEngine.CryptoException;
#endregion

#region License Information
// The MIT License (MIT)
// 
// Copyright (c) 2016 vtdev.com
// This file is part of the CEX Cryptographic library.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// Implementation Details:
// An implementation of a Blum-Blum-Shub random number generator.
// Written by John Underhill, January 05, 2015
// contact: develop@vtdev.com
#endregion

namespace VTDev.Libraries.CEXEngine.Crypto.Prng
{
    /// <summary>
    /// PBPRng: An implementation of a passphrase based PKCS#5 random number generator
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// int x;
    /// using (IRandom rnd = new PBPRng(new SHA512(), PassPhrase, Salt))
    ///     x = rnd.Next();
    /// </code>
    /// </example>
    /// 
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Mac.HMAC"/>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Digest.IDigest"/>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Enumeration.Digests"/>
    /// 
    /// <remarks>
    /// <description>Guiding Publications:</description>
    /// <list type="number">
    /// <item><description>RFC <a href="http://tools.ietf.org/html/rfc2898">2898</a>: Password-Based Cryptography Specification Version 2.0.</description></item>
    /// <item><description>RFC <a href="http://tools.ietf.org/html/rfc2898">2898</a>: Specification.</description></item>
    /// <item><description>NIST <a href="http://csrc.nist.gov/groups/ST/toolkit/rng/documents/SP800-22rev1a.pdf">SP800-22 1a</a>, Section D.3: A Statistical Test Suite for Random and Pseudorandom Number Generators for Cryptographic Applications.</description></item>
    /// <item><description>NIST <a href="http://csrc.nist.gov/publications/drafts/800-90/draft-sp800-90b.pdf">SP800-90B</a>: Recommendation for the Entropy Sources Used for Random Bit Generation.</description></item>
    /// <item><description>NIST <a href="http://csrc.nist.gov/publications/fips/fips140-2/fips1402.pdf">Fips 140-2</a>: Security Requirments For Cryptographic Modules.</description></item>
    /// <item><description>RFC <a href="http://www.ietf.org/rfc/rfc4086.txt">4086</a>: Randomness Requirements for Security.</description></item>
    /// </list>
    /// </remarks>
    public sealed class PBPRng : IRandom
    {
        #region Constants
        private const string ALG_NAME = "PassphrasePrng";
        private const int INT_SIZE = 4;
        private const int LONG_SIZE = 8;
        private const int PKCS_ITERATIONS = 10000;
        #endregion

        #region Fields
        private IDigest m_digest;
        private bool m_disposeEngine = true;
        private bool m_isDisposed = false;
        private int m_position;
        private byte[] m_rndData;
        #endregion

        #region Properties
        /// <summary>
        /// Get: The prngs type name
        /// </summary>
        public Prngs Enumeral
        {
            get { return Prngs.PBPrng; }
        }

        /// <summary>
        /// Algorithm name
        /// </summary>
        public string Name
        {
            get { return ALG_NAME; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new PassphrasePrng from a passphrase and salt,
        /// and seeds it with the output of PBKDF2
        /// </summary>
        /// 
        /// <param name="Digest">Digest engine</param>
        /// <param name="Passphrase">The passphrase</param>
        /// <param name="Salt">The salt value</param>
        /// <param name="Iterations">The number of transformation iterations performed by the digest with PBKDF2 (default is 10,000)</param>
        /// <param name="DisposeEngine">Dispose of digest engine when <see cref="Dispose()"/> on this class is called (default is true)</param>
        /// 
        /// <exception cref="CryptoRandomException">Thrown if a null Digest, Passphrase or Salt are used</exception>
        public PBPRng(IDigest Digest, byte[] Passphrase, byte[] Salt, int Iterations = PKCS_ITERATIONS, bool DisposeEngine = true)
        {
            if (Digest == null)
                throw new CryptoRandomException("PBPRng:Ctor", "Digest can not be null!", new ArgumentNullException());
            if (Passphrase == null)
                throw new CryptoRandomException("PBPRng:Ctor", "Passphrase can not be null!", new ArgumentNullException());
            if (Salt == null)
                throw new CryptoRandomException("PBPRng:Ctor", "Salt can not be null!", new ArgumentNullException());

            try
            {
                m_disposeEngine = DisposeEngine;
                PBKDF2 pkcs = new PBKDF2(Digest, Iterations, false);
                m_digest = Digest;
                pkcs.Initialize(Salt, Passphrase);
                m_rndData = new byte[m_digest.BlockSize];
                pkcs.Generate(m_rndData);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            m_position = 0;
        }

        private PBPRng()
        {
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~PBPRng()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a new Passphrase rng whose output differs but is a
        /// function of this rng's internal state.
        /// </summary>
        /// 
        /// <param name="Digest">The digest instance</param>
        /// 
        /// <returns>Returns a PassphrasePrng instance</returns>
        public PBPRng CreateBranch(IDigest Digest)
        {
            PBPRng branch = new PBPRng();

            try
            {
                branch.m_digest = Digest;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            branch.m_rndData = (byte[])m_rndData.Clone();
            branch.m_rndData[0]++;

            return branch;
        }

        /// <summary>
        /// Fill an array with pseudo random bytes
        /// </summary>
        /// 
        /// <param name="Output">Array to fill with random bytes</param>
        public void GetBytes(byte[] Output)
        {
            int reqSize = Output.Length;
            int algSize = (reqSize % INT_SIZE == 0 ? reqSize : reqSize + INT_SIZE - (reqSize % INT_SIZE));
            int lstBlock = algSize - INT_SIZE;
            int[] rndNum = new int[1];

            for (int i = 0; i < algSize; i += INT_SIZE)
            {
                // get 8 bytes
                rndNum[0] = Next();

                // copy to output
                if (i != lstBlock)
                {
                    // copy in the int bytes
                    Buffer.BlockCopy(rndNum, 0, Output, i, INT_SIZE);
                }
                else
                {
                    // final copy
                    int fnlSize = (reqSize % INT_SIZE) == 0 ? INT_SIZE : (reqSize % INT_SIZE);
                    Buffer.BlockCopy(rndNum, 0, Output, i, fnlSize);
                }
            }
        }

        /// <summary>
        /// Fill an array with pseudo random bytes
        /// </summary>
        /// 
        /// <param name="Size">Size of requested byte array</param>
        /// 
        /// <returns>Random byte array</returns>
        public byte[] GetBytes(int Size)
        {
            byte[] data = new byte[Size];
            GetBytes(data);

            return data;
        }

        /// <summary>
        /// Get a pseudo random 32bit integer
        /// </summary>
        /// 
        /// <returns>Random int</returns>
        public int Next()
        {
            int value = 0;
            for (int i = 0; i < 32; i += 8)
            {
                if (m_position >= m_rndData.Length)
                {
                    m_rndData = m_digest.ComputeHash(m_rndData);
                    m_position = 0;
                }
                value = (value << 8) | (m_rndData[m_position] & 0xFF);
                m_position++;
            }

            return value;
        }

        /// <summary>
        /// Get a ranged pseudo random 32bit integer
        /// </summary>
        /// 
        /// <param name="Maximum">Maximum value</param>
        /// 
        /// <returns>Random int</returns>
        public int Next(int Maximum)
        {
            byte[] rand;
            int[] num = new int[1];

            do
            {
                rand = GetByteRange(Maximum);
                Buffer.BlockCopy(rand, 0, num, 0, rand.Length);
            } while (num[0] > Maximum);

            return num[0];
        }

        /// <summary>
        /// Get a ranged pseudo random 32bit integer
        /// </summary>
        /// 
        /// <param name="Minimum">Minimum value</param>
        /// <param name="Maximum">Maximum value</param>
        /// 
        /// <returns>Random int</returns>
        public int Next(int Minimum, int Maximum)
        {
            int num = 0;
            while ((num = Next(Maximum)) < Minimum) { }
            return num;
        }

        /// <summary>
        /// Get a pseudo random 64bit integer
        /// </summary>
        /// 
        /// <returns>Random long</returns>
        public long NextLong()
        {
            long[] data = new long[1];
            Buffer.BlockCopy(GetBytes(8), 0, data, 0, LONG_SIZE);

            return data[0];
        }

        /// <summary>
        /// Get a ranged pseudo random 64bit integer
        /// </summary>
        /// 
        /// <param name="Maximum">Maximum value</param>
        /// 
        /// <returns>Random long</returns>
        public long NextLong(long Maximum)
        {
            byte[] rand;
            long[] num = new long[1];

            do
            {
                rand = GetByteRange(Maximum);
                Buffer.BlockCopy(rand, 0, num, 0, rand.Length);
            } while (num[0] > Maximum);

            return num[0];
        }

        /// <summary>
        /// Get a ranged pseudo random 64bit integer
        /// </summary>
        /// 
        /// <param name="Minimum">Minimum value</param>
        /// <param name="Maximum">Maximum value</param>
        /// 
        /// <returns>Random long</returns>
        public long NextLong(long Minimum, long Maximum)
        {
            long num = 0;
            while ((num = NextLong(Maximum)) < Minimum) { }
            return num;
        }

        /// <summary>
        /// Sets or resets the internal state
        /// </summary>
        public void Reset()
        {
            Array.Clear(m_rndData, 0, m_rndData.Length);
            m_digest.Reset();
        }
        #endregion

        #region Private Methods
        private byte[] GetByteRange(long Maximum)
        {
            byte[] data;

            if (Maximum < 256)
                data = GetBytes(1);
            else if (Maximum < 65536)
                data = GetBytes(2);
            else if (Maximum < 16777216)
                data = GetBytes(3);
            else if (Maximum < 4294967296)
                data = GetBytes(4);
            else if (Maximum < 1099511627776)
                data = GetBytes(5);
            else if (Maximum < 281474976710656)
                data = GetBytes(6);
            else if (Maximum < 72057594037927936)
                data = GetBytes(7);
            else
                data = GetBytes(8);

            return GetBits(data, Maximum);
        }

        private byte[] GetBits(byte[] Data, long Maximum)
        {
            ulong[] val = new ulong[1];
            Buffer.BlockCopy(Data, 0, val, 0, Data.Length);
            int bits = Data.Length * 8;

            while (val[0] > (ulong)Maximum && bits > 0)
            {
                val[0] >>= 1;
                bits--;
            }

            byte[] ret = new byte[Data.Length];
            Buffer.BlockCopy(val, 0, ret, 0, Data.Length);

            return ret;
        }
        #endregion

        #region IDispose
        /// <summary>
        /// Dispose of this class
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool Disposing)
        {
            if (!m_isDisposed && Disposing)
            {
                if (m_digest != null && m_disposeEngine)
                {
                    m_digest.Dispose();
                    m_digest = null;
                }
                if (m_rndData != null)
                {
                    Array.Clear(m_rndData, 0, m_rndData.Length);
                    m_rndData = null;
                }
                m_isDisposed = true;
            }
        }
        #endregion
    }
}
