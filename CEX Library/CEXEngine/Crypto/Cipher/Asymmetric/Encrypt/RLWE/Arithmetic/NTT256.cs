﻿#region Directives
using System;
using System.Threading.Tasks;
using VTDev.Libraries.CEXEngine.Crypto.Common;
using VTDev.Libraries.CEXEngine.Crypto.Prng;
using VTDev.Libraries.CEXEngine.Utility;
#endregion

namespace VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Encrypt.RLWE.Arithmetic
{
    internal sealed class NTT256
    {
        #region Constants
        private const int NEW_RND_BOTTOM = 1;
        private const int NEW_RND_LARGE = 32 - 9;
        private const int NEW_RND_MID = 32 - 6;
        private const int HAMMING_TABLE_SIZE = 8;
        private const int PMAT_MAX_COL = 109;
        private const int KN_DISTANCE1_MASK = 7;
        private const int KN_DISTANCE2_MASK = 15;
        private const int MODULUS = 7681;           // q
        private const int M = 256;                  // n
        private const int QBY2 = 3840;              // encoding x2
        private const int QBY4 = 1920;              // encoding
        private const int QBY4_TIMES3 = 5760;       // encode max
        private const int FWD_CONST1 = 5118;        // primrt in fwdntt2
        private const int FWD_CONST2 = 1065;        // omega in fwdntt
        private const int INVCONST1 = 2880;         // primert1 in invntt2
        private const int INVCONST2 = 3383;         // omega2 in invntt2
        private const int INVCONST3 = 2481;         // primert2 in invntt2
        private const int SCALING = 7651;
        #endregion

        #region Fields
        IRandom _secRand;

        private static readonly ushort[] _primeRtOmega = 
        { 
            7680, 4298, 6468, 849, 2138, 3654, 1714, 5118 
        };

        private static ushort[] _invPrimeRt = 
        {
            7680, 3383, 5756, 1728, 7584, 6569, 6601
        };

        private static readonly byte[] _T1 =
        {
            3,4,1,2,2,8,6,1,3,0,1,9,2,5,5,4,3,4,1,1,2,7,6,11,3,0,1,4,2,4,5,2,3,4,1,2,2,8,6,0,3,0,1,7,2,5,
            5,12,3,4,1,1,2,7,6,9,3,0,1,3,2,4,5,19,3,4,1,2,2,8,6,1,3,0,1,9,2,5,5,0,3,4,1,1,2,7,6,10,3,0,1,
            4,2,4,5,17,3,4,1,2,2,8,6,0,3,0,1,7,2,5,5,8,3,4,1,1,2,7,6,6,3,0,1,3,2,4,5,21,3,4,1,2,2,8,6,1,
            3,0,1,9,2,5,5,4,3,4,1,1,2,7,6,11,3,0,1,4,2,4,5,16,3,4,1,2,2,8,6,0,3,0,1,7,2,5,5,10,3,4,1,1,2,
            7,6,9,3,0,1,3,2,4,5,20,3,4,1,2,2,8,6,1,3,0,1,9,2,5,5,0,3,4,1,1,2,7,6,10,3,0,1,4,2,4,5,18,3,4,
            1,2,2,8,6,0,3,0,1,7,2,5,5,7,3,4,1,1,2,7,6,6,3,0,1,3,2,4,5,22 
        };

        private static readonly byte[] _T2 =
        {
            13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,13,10,7,6,7,6,7,6,7,6,7,6,
            7,6,7,6,7,6,7,6,7,6,7,6,7,6,7,6,7,6,7,6,7,6,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,5,4,0,14,0,12,
            0,14,0,12,0,14,0,12,0,14,0,12,0,14,0,12,0,14,0,12,0,14,0,12,0,14,0,12,11,8,10,3,11,8,10,3,11,8,10,3,11,8,10,3,11,8,10,
            3,11,8,10,3,11,8,10,3,11,8,10,3,15,8,10,1,13,6,9,16,15,8,10,1,13,6,9,14,15,8,10,1,13,6,9,16,15,8,10,1,13,6,9,14,13,14,
            7,35,11,0,0,39,12,7,6,37,9,33,17,41,13,8,7,36,11,32,0,40,12,4,6,38,9,34,15,42
        };

        private static readonly int[][] _pMatrix = 
        {
            new int[] {0,0,0,1,0,1,1,0,1,0,0,1,1,1,0,1,0,1,1,0,0,1,1,0,0,0,1,1,1,0,0,0,0,0,0,1,0,1,0,1,1,0,1,1,0,1,0,1,1,1,0,1,0,0,1,0,1,1,0,0,0,1,0,1,0,0,1,1,0,0,1,1,1,0,0,0,1,1,1,1,1,1,0,1,0,1,0,0,1,0,0,1,0,1,0,0,0,1,0,1,1,0,1,0,0,1,1,1,0},
            new int[] {0,0,1,0,1,1,0,0,0,0,1,0,0,0,1,0,0,1,1,0,0,0,0,1,0,0,1,0,0,0,0,0,0,1,1,1,1,0,1,1,0,1,0,1,1,1,1,0,1,0,1,1,0,0,0,1,0,1,0,1,0,0,1,1,1,1,1,1,1,1,1,1,1,1,0,1,1,0,0,0,0,0,0,1,1,1,1,0,0,1,1,1,1,1,1,1,0,0,1,0,0,1,0,1,0,0,1,0,0},
            new int[] {0,0,1,0,1,0,0,1,0,0,0,0,0,0,0,1,0,0,1,1,0,1,1,1,0,0,1,1,0,1,0,0,1,0,0,0,1,1,1,0,0,1,1,1,0,0,1,0,1,1,0,1,0,1,1,1,1,1,0,0,0,1,0,0,0,0,1,0,0,0,1,1,1,1,1,1,1,0,1,0,1,1,1,0,0,0,1,1,0,1,1,1,0,0,1,1,1,0,0,1,0,1,0,1,1,1,1,1,1},
            new int[] {0,0,1,0,0,1,0,0,0,1,0,0,0,1,1,0,0,0,1,0,1,1,1,1,1,0,1,1,0,0,1,1,0,0,1,1,1,1,0,0,0,0,1,1,0,0,0,0,1,1,1,0,1,0,0,0,0,1,0,0,1,1,1,0,0,1,0,0,0,0,0,0,1,0,0,0,1,1,1,0,1,1,0,1,0,0,0,1,1,0,0,1,0,0,1,1,0,0,1,0,0,0,0,1,1,0,0,1,0},
            new int[] {0,0,0,1,1,1,1,0,1,0,0,0,1,1,0,1,1,1,0,0,1,0,0,1,0,1,1,1,0,0,0,0,0,0,1,1,1,1,0,0,0,1,0,0,1,0,0,1,0,0,1,0,0,1,1,0,1,0,0,1,0,1,1,0,0,0,0,1,0,1,1,1,0,0,0,0,0,0,1,1,1,0,0,0,1,0,1,0,0,0,0,1,0,0,0,1,1,1,1,0,1,0,0,1,1,0,0,1,0},
            new int[] {0,0,0,1,1,0,0,0,1,0,0,0,0,0,0,1,0,0,0,1,0,0,0,0,1,1,1,1,0,0,1,1,1,1,0,1,0,0,1,1,0,0,0,0,1,1,1,1,1,1,0,0,1,1,1,1,0,0,1,0,0,1,1,0,1,0,1,1,0,1,0,0,0,0,0,0,1,0,1,1,0,0,1,1,0,1,1,1,0,0,1,1,0,0,0,1,0,1,1,0,0,1,0,1,1,1,1,0,1},
            new int[] {0,0,0,1,0,0,1,0,1,0,1,1,0,1,1,0,0,1,0,0,0,0,1,0,1,0,0,0,1,0,0,0,0,0,0,1,0,0,1,0,0,1,1,1,0,0,0,1,0,1,1,1,0,1,0,0,0,1,1,1,0,0,1,0,0,1,1,1,0,1,1,1,0,1,1,1,1,0,1,0,1,0,1,1,0,1,1,1,0,0,0,0,1,0,0,1,1,1,1,0,1,1,1,0,1,0,0,0,0},
            new int[] {0,0,0,0,1,1,0,1,1,0,0,1,1,0,1,0,1,1,1,0,1,0,1,1,1,0,1,1,0,1,0,0,1,1,0,0,1,1,0,0,1,0,1,1,0,0,0,1,0,0,0,1,1,0,0,0,0,0,0,1,1,1,1,1,1,1,1,0,0,1,1,0,1,0,1,0,0,0,0,0,0,0,0,0,1,1,1,0,1,1,1,1,0,1,1,1,0,0,1,0,1,0,0,0,1,1,0,1,0},
            new int[] {0,0,0,0,1,0,0,1,0,1,1,0,1,0,1,1,0,0,1,1,0,0,1,1,0,1,1,1,0,1,0,0,0,0,1,0,0,1,1,1,0,1,0,0,1,1,0,0,0,1,0,0,1,1,1,0,1,0,0,0,1,0,1,1,1,0,1,1,1,1,0,0,0,1,1,1,1,1,1,0,0,0,1,1,1,0,0,0,0,1,0,1,0,1,1,1,1,1,1,1,1,0,1,0,0,0,1,0,1},
            new int[] {0,0,0,0,0,1,1,0,0,0,1,1,0,1,0,1,0,1,1,0,0,0,1,0,0,0,0,0,1,0,0,0,0,1,0,0,1,1,1,0,1,0,0,1,0,1,1,1,1,1,1,0,0,1,1,1,0,0,1,0,0,0,1,0,0,0,1,0,1,1,1,0,1,1,1,1,0,1,0,1,1,0,0,1,0,1,1,1,0,1,0,1,1,1,1,0,1,1,1,0,0,1,0,0,1,0,1,1,0},
            new int[] {0,0,0,0,0,0,1,1,1,1,1,0,0,1,0,1,1,0,0,0,1,0,0,1,0,1,1,1,0,1,0,1,0,1,0,1,1,0,1,1,0,0,1,0,1,1,0,0,0,1,0,1,0,0,0,1,0,0,0,0,1,0,1,1,1,1,1,1,0,1,0,0,0,0,1,0,1,1,0,0,0,1,1,0,1,1,0,1,1,1,0,0,0,0,1,1,0,0,1,0,0,0,0,0,0,0,1,1,1},
            new int[] {0,0,0,0,0,0,1,0,0,1,0,1,0,1,0,0,0,0,0,1,1,1,1,1,0,0,0,1,0,1,1,0,1,1,0,0,1,1,1,1,0,1,1,0,0,1,1,1,1,0,1,1,0,0,1,0,1,1,1,0,1,1,1,0,1,0,0,1,0,0,1,1,0,0,0,0,0,1,0,1,0,0,0,1,0,1,1,0,0,0,1,1,1,0,1,1,1,0,0,1,1,0,1,0,1,0,0,1,0},
            new int[] {0,0,0,0,0,0,0,1,0,1,0,1,0,0,1,1,0,0,1,1,0,0,0,1,0,0,1,1,0,0,0,1,0,1,1,0,1,1,0,0,1,0,0,1,1,0,1,0,0,0,0,1,1,1,0,0,0,0,0,1,1,0,0,0,1,0,1,0,1,1,0,1,1,0,1,1,0,1,1,0,0,1,0,1,0,1,0,0,1,0,0,1,1,0,1,0,1,1,1,1,0,1,1,0,0,1,1,0,1},
            new int[] {0,0,0,0,0,0,0,0,1,0,1,1,0,1,1,1,1,1,0,0,0,0,1,1,1,1,0,0,0,1,0,1,1,1,0,1,0,1,1,1,0,1,1,1,0,0,1,1,1,0,0,1,1,0,0,1,0,0,0,0,0,0,0,1,1,1,1,1,1,0,0,1,1,0,0,0,1,0,1,1,0,0,0,1,0,1,0,0,1,0,0,1,1,1,1,0,0,1,0,0,0,0,0,0,1,0,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,1,0,1,1,1,1,0,1,1,0,0,1,0,1,1,0,1,1,0,1,1,1,0,1,0,1,0,1,0,1,0,0,1,1,1,1,0,0,1,1,0,0,0,0,0,0,0,1,1,0,0,0,0,0,1,1,1,1,1,1,1,0,0,0,1,0,1,0,0,1,0,0,0,0,1,1,1,0,0,0,1,0,1,1,0,1,1,0,1,0,0,1,0,1,1,1,0,0,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,1,0,1,1,1,0,1,0,0,0,1,1,1,1,0,1,0,0,1,1,0,1,0,1,0,0,0,1,1,1,0,0,1,0,1,1,1,1,1,0,1,1,0,1,0,1,1,0,1,1,1,0,1,0,1,0,0,0,0,0,1,0,1,0,0,0,0,0,0,0,1,1,0,1,1,0,0,0,1,0,0,0,0,0,1,1,0,0,0,0,0,0,1,0,1,1,0,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1,1,1,0,0,0,1,1,0,0,0,1,1,0,1,0,1,0,0,0,1,0,1,1,0,0,0,0,0,0,0,1,0,1,0,1,1,0,1,1,1,1,1,0,0,0,1,0,1,0,1,0,0,0,0,0,0,1,1,1,1,1,1,1,0,1,0,0,0,0,1,1,0,1,1,0,1,0,1,1,0,1,1,0,1,1,1,0,0,1,1,1,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,1,0,1,1,0,0,1,0,0,0,0,1,1,0,1,1,1,0,1,1,1,0,1,1,1,1,0,1,0,0,0,0,1,0,1,1,0,1,1,0,1,1,1,1,0,0,1,0,0,0,1,0,1,0,0,0,0,1,0,1,0,1,1,0,0,0,0,0,0,0,0,1,1,0,0,0,0,1,0,1,0,0,1,1,1,0,0,1,0,1,0,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,1,1,0,0,0,1,0,1,1,0,1,1,1,0,1,1,1,1,0,1,0,1,0,1,1,0,0,0,1,0,1,0,0,0,1,1,0,1,1,1,0,0,0,0,1,0,0,1,1,1,0,0,1,1,0,0,1,0,0,1,0,0,0,1,0,1,0,0,1,0,0,1,1,0,1,0,1,1,1,0,1,0,1,0,0,1,1,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,1,0,1,0,0,0,1,1,0,1,0,0,1,1,1,0,1,0,0,1,1,1,1,1,1,0,0,0,0,1,1,0,1,0,1,0,0,1,1,1,0,0,1,0,1,1,0,0,1,0,0,1,0,0,1,1,0,1,1,0,0,0,1,1,0,0,1,0,0,0,1,0,0,1,0,1,1,1,1,0,1,0,0,0,0,1,0,1,0,0,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,0,0,1,1,0,1,0,0,1,0,1,0,1,1,0,0,1,1,0,1,0,1,0,0,1,1,1,0,1,0,0,1,0,0,1,1,0,0,1,0,1,1,1,0,0,0,0,0,1,1,1,0,0,0,0,0,0,1,0,0,0,1,1,0,1,0,1,1,1,0,1,1,0,1,0,1,1,0,0,0,0,0,1,1,0,0,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0,1,1,1,1,0,0,0,0,1,1,0,0,0,0,1,0,1,0,1,1,0,1,0,0,0,1,0,0,0,1,0,0,0,1,0,0,1,1,0,1,1,0,1,1,1,1,0,0,0,0,1,0,1,0,1,0,1,1,0,1,1,0,0,0,1,1,1,0,0,0,0,0,0,0,0,0,1,0,1,1,0,1,0,1,0,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,0,1,1,0,1,0,0,1,1,0,0,1,1,1,1,0,1,1,1,1,1,1,0,1,0,0,1,0,1,1,0,0,0,0,1,0,1,1,1,1,0,1,1,1,1,0,0,1,0,1,1,0,1,1,1,1,0,1,1,1,0,0,0,1,0,1,1,0,0,0,0,1,1,0,1,0,0,1,0,1,0,1,1,0,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,1,1,1,0,1,0,0,0,1,1,1,0,0,0,0,0,1,1,0,0,0,0,1,1,0,1,0,1,1,1,0,1,0,1,1,0,1,0,0,1,0,0,1,0,1,0,0,1,0,1,1,1,0,0,1,0,1,1,1,0,0,0,1,1,0,1,0,0,1,1,1,0,0,1,1,0,0,1,0,1,0,1,1,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,1,1,1,0,1,1,0,1,0,0,1,0,0,0,1,0,0,1,1,1,1,1,0,1,1,0,1,1,1,0,0,0,1,0,1,0,0,1,0,1,1,1,1,1,1,1,1,0,1,1,0,0,1,1,1,1,1,0,1,0,0,1,1,1,1,1,0,1,1,0,1,0,0,1,1,1,0,0,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1,0,0,0,0,0,0,1,1,0,0,0,0,0,0,1,1,1,0,1,1,1,1,1,0,1,0,0,1,1,1,0,1,0,0,0,0,1,1,0,0,0,0,1,0,0,0,1,0,1,0,1,0,0,1,1,0,0,0,0,1,0,1,1,0,1,0,1,1,0,0,0,1,1,1,1,0,1,1,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,1,0,0,1,0,0,1,0,1,1,1,1,1,1,1,1,1,1,0,1,0,0,1,0,0,1,1,1,1,0,0,0,0,0,0,1,0,1,0,1,0,0,0,0,0,0,0,1,0,1,1,0,1,1,0,1,0,0,0,0,1,1,0,0,1,0,1,0,0,0,1,0,1,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,1,0,0,1,0,0,0,0,1,0,0,0,0,0,0,0,0,1,0,0,1,1,1,0,1,0,1,0,0,1,0,1,0,0,0,0,1,0,1,1,0,0,0,1,0,0,0,1,0,1,1,1,0,0,0,1,0,0,0,1,1,0,1,0,0,0,0,0,1,1,1,0,0,1,0,0,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,1,1,0,1,0,0,0,1,0,1,1,0,0,1,1,0,0,1,0,0,0,0,1,1,0,0,1,1,1,1,0,1,1,1,0,1,1,0,0,0,1,0,0,1,0,1,0,0,0,1,0,0,0,0,1,1,0,0,0,0,0,1,1,0,1,0,1,1,0,1,0,1,1,1,0,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,1,0,1,1,1,1,1,0,0,0,0,0,1,1,1,1,1,0,1,0,0,0,1,0,1,0,1,1,1,0,0,1,0,0,1,1,1,0,1,1,1,0,1,1,0,0,1,1,1,0,1,1,1,1,1,1,1,1,1,0,0,0,1,0,0,1,1,1,1,1,0,0,0,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,1,0,1,1,0,0,1,0,1,0,0,0,0,1,0,1,0,0,0,0,0,0,1,0,1,1,0,1,0,1,0,0,1,0,0,0,0,0,1,1,0,1,1,1,0,1,0,1,1,0,0,0,1,1,0,0,0,1,0,1,1,0,1,1,1,0,1,0,1,0,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,1,0,1,1,0,0,0,1,0,0,0,1,1,1,1,0,0,0,1,1,0,1,1,0,1,1,1,0,0,1,1,1,1,1,1,0,0,1,1,1,0,0,1,1,1,0,0,1,0,0,0,1,0,0,0,0,1,1,1,1,0,0,1,0,1,1,0,1,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,1,0,1,1,0,1,1,1,1,0,1,1,1,0,0,1,0,0,0,0,0,1,0,1,1,1,0,0,1,0,0,0,1,0,1,0,1,0,0,1,0,0,1,1,1,1,1,1,1,1,0,0,0,0,1,0,0,0,1,1,1,1,1,0,1,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,0,0,1,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,1,0,0,0,1,1,0,1,0,1,1,0,0,1,1,1,0,0,0,1,0,0,0,0,1,0,0,1,1,1,1,0,1,1,0,0,1,1,0,1,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0,1,1,1,0,0,1,1,0,1,1,0,0,1,0,1,0,1,0,0,0,1,0,0,0,1,1,0,0,1,0,0,1,1,0,0,1,0,0,1,0,1,1,0,0,0,0,1,0,0,0,1,0,0,1,1,1,1,0,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,0,0,0,0,0,0,1,0,0,0,1,1,0,0,0,1,0,1,0,1,0,0,0,1,0,0,1,1,0,0,0,1,0,1,1,1,1,1,0,0,0,1,0,1,1,1,1,0,1,1,0,0,1,1,1,1,0,1,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,1,0,1,0,0,1,0,0,0,0,0,1,1,1,0,1,1,0,1,0,1,1,0,0,0,0,0,0,1,1,0,1,1,0,0,0,0,1,1,1,0,1,1,0,0,0,0,0,0,1,0,1,0,1,1,0,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,1,1,1,0,0,0,1,1,1,0,1,1,1,0,0,1,0,0,0,1,0,1,1,0,1,0,1,1,1,1,1,1,0,0,0,0,0,0,0,1,0,1,0,0,1,0,0,0,1,1,1,1,0,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1,0,1,1,1,1,1,0,0,0,1,1,0,1,1,1,1,0,0,1,0,0,0,1,1,0,1,0,0,1,1,0,1,0,1,1,1,1,0,1,0,0,0,1,1,1,1,1,1,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,1,0,0,0,0,0,0,1,1,0,1,0,0,1,0,0,0,0,0,1,0,1,0,1,1,1,1,0,0,1,1,1,0,0,1,0,0,0,0,1,0,1,0,1,1,1,0,1,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,1,1,1,1,1,1,1,0,0,0,1,1,0,1,0,0,0,0,1,1,0,1,1,1,0,0,1,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,1,1,1,0,1,1,0,0,0,1,0,0,0,0,0,1,1,1,0,1,1,1,1,0,0,0,0,0,1,0,1,0,1,0,0,0,0,1,0,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,0,1,1,1,0,1,1,0,1,1,0,0,1,1,0,1,0,0,0,1,0,0,1,0,0,0,1,0,0,0,1,1,1,0,1,1,0,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,0,1,0,0,1,1,1,1,1,0,0,1,0,1,1,1,0,1,1,1,1,0,1,1,0,0,0,0,0,1,0,0,0,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,1,1,0,0,0,1,0,0,0,0,0,1,1,0,1,1,1,1,1,1,0,0,0,1,1,1,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0,1,0,1,0,1,1,0,1,0,1,1,0,1,0,1,0,1,0,1,1,1,0,0,0,1,1,1,1,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,1,0,0,1,1,1,0,1,0,0,0,0,0,1,0,1,1,0,0,0,1,1,1,1,0,1,1,0},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,0,1,0,1,0,0,1,0,0,0,1,0,0,1,1,1,0,0,0,0,0,0,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,1,0,1,0,0,1,1,1,1,1,1,1,0,0,0,0,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,1,1,1,1,0,1,1,1,0,0,0,0,0,1,1,1,1,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,1,1,0,1,1,0,1,0,0,1,0,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,1,0,1,0,1,0,1,0,1,0,1,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0,1,0,0,0,0,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,1,0,1},
            new int[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0}
        };
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize this class
        /// </summary>
        /// 
        /// <param name="SecRand">The secure random number generator instance</param>
        public NTT256(IRandom SecRand)
        {
            _secRand = SecRand;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Decrypt a ciphertext
        /// </summary>
        /// 
        /// <param name="PrivateKey">The RLWE private key</param>
        /// <param name="Message">The encrypted message</param>
        /// 
        /// <returns>The decrypted message</returns>
        public byte[] Decrypt(RLWEPrivateKey PrivateKey, byte[] Message)
        {
            uint[] lmsg = new uint[M];
            uint[] lmsg2 = new uint[M];

            uint[][] cpt = ArrayUtils.Split(Convert8To32(Message), Message.Length / 4);
            RLWEDecrypt(cpt[0], cpt[1], Convert8To32(PrivateKey.R2));
            QBDecode(cpt[0]);
            ArrangeFinal(cpt[0], lmsg2);

            return Decode(lmsg2);
        }

        /// <summary>
        /// Encrypt a message
        /// </summary>
        /// 
        /// <param name="PublicKey">The RLWE public key</param>
        /// <param name="Message">The message array to encrypt</param>
        /// 
        /// <returns>The encrypted message</returns>
        public byte[] Encrypt(RLWEPublicKey PublicKey, byte[] Message)
        {
            uint[] lmsg = new uint[M];
            uint[] cpt1 = new uint[M];
            uint[] cpt2 = new uint[M];

            // bit encoding
            lmsg = Encode(Message);
            // reverse msg
            BitReverse(lmsg);
            // pub a + p, message m, ciphertest c1 + c2
            RLWEEncrypt(Convert8To32(PublicKey.A), cpt1, cpt2, lmsg, Convert8To32(PublicKey.P));

            return ArrayUtils.Concat(Convert32To8(cpt1), Convert32To8(cpt2));
        }

        /// <summary>
        /// Generate a RLWE key pair
        /// </summary>
        /// 
        /// <returns>An initialized RLWE KeyPair</returns>
        public RLWEKeyPair Generate()
        {
            uint[] pubA = new uint[M];
            uint[] pubP = new uint[M];
            uint[] priR2 = new uint[M];

            KeyGen(pubA, pubP, priR2);

            RLWEPrivateKey pri = new RLWEPrivateKey(M, Convert32To8(priR2));
            RLWEPublicKey pub = new RLWEPublicKey(M, Convert32To8(pubA), Convert32To8(pubP));

            return new RLWEKeyPair(pub, pri);
        }
        #endregion

        #region Private Methods
        private void ArrangeFinal(uint[] Input, uint[] Output)
        {
            int ctr;
            for (ctr = 0; ctr < M / 2; ctr += 2)
            {
                Output[ctr] = Input[2 * ctr];
                Output[ctr + 1] = Input[2 * (ctr + 1)];
            }

            for (ctr = 0; ctr < M / 2; ctr += 2)
            {
                Output[ctr + M / 2] = Input[2 * ctr + 1];
                Output[ctr + 1 + M / 2] = Input[2 * (ctr + 1) + 1];
            }
        }

        private void BitReverse(uint[] A)
        {
            int b1, b2, b3, b4, b5, b6, b7, b8, swpIndex;
            int q1, r1, q2, r2;
            int temp = 0;

            for (int i = 0; i < M; i++)
            {
                b1 = i % 2;
                b2 = (i >> 1) % 2;
                b3 = (i >> 2) % 2;
                b4 = (i >> 3) % 2;
                b5 = (i >> 4) % 2;
                b6 = (i >> 5) % 2;
                b7 = (i >> 6) % 2;
                b8 = (i >> 7) % 2;

                swpIndex = b1 * 128 + b2 * 64 + b3 * 32 + b4 * 16 + b5 * 8 + b6 * 4 + b7 * 2 + b8;

                q1 = i / 2;
                r1 = i % 2;
                q2 = swpIndex / 2;
                r2 = swpIndex % 2;

                if (swpIndex > i)
                {
                    if (r2 == 0)
                        temp = (int)A[2 * q2];
                    if (r2 == 1)
                        temp = (int)A[2 * q2 + 1];
                    if (r2 == 0 && r1 == 0)
                        A[2 * q2] = A[2 * q1];
                    if (r2 == 0 && r1 == 1)
                        A[2 * q2] = A[2 * q1 + 1];
                    if (r2 == 1 && r1 == 0)
                        A[2 * q2 + 1] = A[2 * q1];
                    if (r2 == 1 && r1 == 1)
                        A[2 * q2 + 1] = A[2 * q1 + 1];
                    if (r1 == 0)
                        A[2 * q1] = (uint)temp;
                    if (r1 == 1)
                        A[2 * q1 + 1] = (uint)temp;
                }
            }
        }

        private uint Clz(uint A)
        {
            for (int i = 0; i < 32; i++)
            {
                if (IntUtils.URShift(A, (31 - i)) == 1)
                    return (uint)i;
            }
            return 32;
        }

        private byte[] Convert32To8(uint[] Source)
        {
            byte[] data = new byte[Source.Length * 2];

            for (int i = 0, j = 0; i < data.Length; i += 2, j += 4)
                Buffer.BlockCopy(Source, j, data, i, 2);

            return data;
        }

        private uint[] Convert8To32(byte[] Source)
        {
            uint[] data = new uint[Source.Length / 2];

            for (int i = 0, j = 0; i < Source.Length; i += 2, j += 4)
                Buffer.BlockCopy(Source, i, data, j, 2);

            return data;
        }

        private byte[] Decode(uint[] A)
        {
            byte[] r = new byte[A.Length / 8];
            for (int i = 0, j = 0; i < r.Length; i++, j += 8)
            {
                r[i] = (byte)((A[j]) << 7 |
                    (A[j + 1]) << 6 |
                    (A[j + 2]) << 5 |
                    (A[j + 3]) << 4 |
                    (A[j + 4]) << 3 |
                    (A[j + 5]) << 2 |
                    (A[j + 6]) << 1 |
                    A[j + 7]);

            }
            return r;
        }

        private uint[] Encode(byte[] A)
        {
            uint[] r = new uint[A.Length * 8];
            for (int i = 0, j = 0; i < A.Length; i++, j += 8)
            {
                r[j] = (uint)A[i] >> 7 & 1;
                r[j + 1] = (uint)A[i] >> 6 & 1;
                r[j + 2] = (uint)A[i] >> 5 & 1;
                r[j + 3] = (uint)A[i] >> 4 & 1;
                r[j + 4] = (uint)A[i] >> 3 & 1;
                r[j + 5] = (uint)A[i] >> 2 & 1;
                r[j + 6] = (uint)A[i] >> 1 & 1;
                r[j + 7] = (uint)A[i] & 1;
            }
            return r;
        }

        private void FwdNTT(uint[] A)
        {
            int i, j, k, m;
            int u1, t1, u2, t2;
            int primrt, omega;

            i = 0;
            for (m = 2; m <= M / 2; m = 2 * m)
            {
                primrt = _primeRtOmega[i];
                omega = _primeRtOmega[i + 1];
                i++;

                for (j = 0; j < m; j += 2)
                {
                    for (k = 0; k < M; k = k + 2 * m)
                    {
                        u1 = (int)A[j + k];
                        t1 = (int)Mod(omega * (int)A[j + k + 1]);
                        u2 = (int)A[j + k + m];
                        t2 = (int)Mod(omega * (int)A[j + k + m + 1]);
                        A[j + k] = Mod(u1 + t1);
                        A[j + k + 1] = Mod(u2 + t2);
                        A[j + k + m] = Mod(u1 - t1);
                        A[j + k + m + 1] = Mod(u2 - t2);
                    }
                    omega = omega * primrt;
                    omega = (int)Mod(omega);
                }
            }

            primrt = FWD_CONST1;
            omega = FWD_CONST2;

            for (j = 0; j < M / 2; j++)
            {
                t1 = omega * (int)A[2 * j + 1];
                t1 = (int)Mod(t1);
                u1 = (int)A[2 * j];
                A[2 * j] = (uint)(u1 + t1);
                A[2 * j] = Mod((int)A[2 * j]);
                A[2 * j + 1] = (uint)(u1 - t1);
                A[2 * j + 1] = Mod((int)A[2 * j + 1]);
                omega = omega * primrt;
                omega = (int)Mod(omega);
            }
        }

        private void GenA(uint[] A)
        {
            int ctr, rand;

            for (ctr = 0; ctr < M / 2; ctr++)
            {
                rand = (int)GetRand();
                A[2 * ctr] = Mod(rand & 0xFFFF);
                A[2 * ctr + 1] = Mod(IntUtils.URShift(rand, 16));
            }

            FwdNTT(A);
        }

        private void GenR1(uint[] R1)
        {
            KnuthYao(R1);
            FwdNTT(R1);
        }

        private void GenR2(uint[] R2)
        {
            int rand, bit, sign;

            for (int i = 0; i < M; )
            {
                rand = (int)GetRand();

                for (int j = 0; j < 16; j++)
                {
                    bit = rand & 1;
                    sign = IntUtils.URShift(rand, 1) & 1;
                    if (sign == 1 && bit == 1)
                        bit = (MODULUS - 1);
                    R2[i++] = (uint)bit;
                    rand = IntUtils.URShift(rand, 2);
                }
            }

            FwdNTT(R2);
        }

        private uint GetRand()
        {
            uint rnd = (uint)_secRand.Next();
            // set the least significant bit
            rnd |= 0x80000000;

            return rnd;
        }

        private void InvNTT(uint[] A)
        {
            int i, j, k, m;
            int u1, t1, u2, t2;
            int primrt = 0, omega = 0;

            for (m = 2, i = 0; m <= M / 2; m = 2 * m, i++)
            {
                primrt = _invPrimeRt[i];
                omega = 1;

                for (j = 0; j < m / 2; j++)
                {
                    for (k = 0; k < M / 2; k = k + m)
                    {
                        t1 = omega * (int)A[2 * (k + j) + 1];
                        t1 = (int)Mod(t1);
                        u1 = (int)A[2 * (k + j)];
                        t2 = omega * (int)A[2 * (k + j + m / 2) + 1];
                        t2 = (int)Mod(t2);
                        u2 = (int)A[2 * (k + j + m / 2)];
                        A[2 * (k + j)] = (uint)(u1 + t1);
                        A[2 * (k + j)] = Mod((int)A[2 * (k + j)]);
                        A[2 * (k + j + m / 2)] = (uint)(u1 - t1);
                        A[2 * (k + j + m / 2)] = Mod((int)A[2 * (k + j + m / 2)]);
                        A[2 * (k + j) + 1] = (uint)(u2 + t2);
                        A[2 * (k + j) + 1] = Mod((int)A[2 * (k + j) + 1]);
                        A[2 * (k + j + m / 2) + 1] = (uint)(u2 - t2);
                        A[2 * (k + j + m / 2) + 1] = Mod((int)A[2 * (k + j + m / 2) + 1]);
                    }
                    omega = omega * primrt;
                    omega = (int)Mod(omega);
                }
            }

            primrt = INVCONST1;
            omega = 1;

            for (j = 0; j < M;)
            {
                u1 = (int)A[j];
                j++;
                t1 = (int)(omega * A[j]);
                t1 = (int)Mod(t1);
                A[j - 1] = (uint)(u1 + t1);
                A[j - 1] = Mod((int)A[j - 1]);
                A[j] = (uint)(u1 - t1);
                A[j] = Mod((int)A[j++]);
                omega = omega * primrt;
                omega = (int)Mod(omega);
            }

            int omega2 = INVCONST2;
            primrt = INVCONST3;
            omega = 1;

            for (j = 0; j < M;)
            {
                A[j] = (uint)omega * A[j];
                A[j] = Mod((int)A[j]);
                A[j] = A[j] * SCALING;
                A[j] = Mod((int)A[j++]);
                A[j] = (uint)omega2 * A[j];
                A[j] = Mod((int)A[j]);
                A[j] = A[j] * SCALING;
                A[j] = Mod((int)A[j++]);

                omega = omega * primrt;
                omega = (int)Mod(omega);
                omega2 = omega2 * primrt;
                omega2 = (int)Mod(omega2);
            }
        }

        private void KeyGen(uint[] A, uint[] P, uint[] R2)
        {
            if (ParallelUtils.IsParallel)
            {
                Action[] gA = new Action[]
                { 
                    new Action(()=> GenA(A)), 
                    new Action(()=> GenR1(P)), 
                    new Action(()=> GenR2(R2))
                };
                Parallel.Invoke(gA);
            }
            else
            {
                GenA(A);
                GenR1(P);
                GenR2(R2);
            }

            uint[] tmpA = new uint[M];
            //a = a*r2
            FFT.Mul2(tmpA, A, R2, MODULUS);
            //p = p-a*r2
            FFT.Sub2(P, P, tmpA, MODULUS);

            Rearrange(R2);
        }

        private void KnuthYao(uint[] A)
        {
            uint rnd = GetRand();

            for (int i = 0; i < M / 2; i++)
            {
                A[2 * i + 1] = KnuthYaoSingle(ref rnd);
                A[2 * i] = KnuthYaoSingle(ref rnd);
            }
        }

        private uint KnuthYaoSingle(ref uint Rand)
        {
            int distance;
            int row, column;
            int index = (int)Rand & 0xFF;
            Rand >>= 8;
            int sample = _T1[index];
            int sampleMsb = sample & 16;

            // lookup was successful
            if (sampleMsb == 0)
            {
                if (Rand == NEW_RND_BOTTOM)
                    Rand = GetRand();

                sample &= 0xF;
                // 9th bit in rnd is the sign
                if ((Rand & 1) != 0)
                    sample = (MODULUS - sample);

                Rand >>= 1;

                // We know that in the next call we will need 8 bits!
                if (Clz(Rand) > (NEW_RND_LARGE))
                    Rand = GetRand();

                return (uint)sample;
            }
            else
            {
                if (Clz(Rand) > (NEW_RND_MID))
                    Rand = GetRand();

                distance = sample & KN_DISTANCE1_MASK;
                index = (int)(Rand & 0x1F) + 32 * distance;
                Rand >>= 5;

                if (Rand == NEW_RND_BOTTOM)
                    Rand = GetRand();

                sample = _T2[index];
                sampleMsb = sample & 32;

                // lookup was successful
                if (sampleMsb == 0)
                {
                    sample = sample & 31;
                    if ((Rand & 1) != 0)
                        sample = (MODULUS - sample);

                    Rand = Rand >> 1;
                    if (Clz(Rand) > (NEW_RND_LARGE))
                        Rand = GetRand();

                    return (uint)sample;
                }
                else
                {
                    //Real knuth-yao
                    distance = sample & KN_DISTANCE2_MASK;
                    for (column = 13; (column < PMAT_MAX_COL); column++)
                    {
                        distance = (int)(distance * 2 + (Rand & 1));
                        Rand = Rand >> 1;
                        if (Rand == NEW_RND_BOTTOM)
                            Rand = GetRand();

                        // Read probability-column 0 and count the number of non-zeros
                        for (row = 54; row >= 0; row--)
                        {
                            distance = distance - _pMatrix[row][column];
                            if (distance < 0)
                            {
                                if ((Rand & 1) != 0)
                                    sample = (MODULUS - row);
                                else
                                    sample = row;

                                Rand = Rand >> 1;
                                if (Clz(Rand) > (NEW_RND_LARGE))
                                    Rand = GetRand();

                                return (uint)sample;
                            }
                        }
                    }
                }
            }

            return 0;
        }

        private uint Mod(int A)
        {
            int quotient, remainder;
            quotient = A / MODULUS;

            if (A >= 0)
                remainder = A - quotient * MODULUS;
            else
                remainder = (1 - quotient) * MODULUS + A;

            return (uint)remainder;
        }

        private void QBDecode(uint[] Cpt1)
        {
            for (int i = 0; i < M; i++)
            {
                if ((Cpt1[i] > QBY4) && (Cpt1[i] < QBY4_TIMES3))
                    Cpt1[i] = 1;
                else
                    Cpt1[i] = 0;
            }
        }

        private void Rearrange(uint[] A)
        {
            int ctr;
            int b1, b2, b3, b4, b5, b6, b7;
            int swpIndex;

            int u1, u2;

            for (ctr = 1; ctr < A.Length / 2; ctr++)
            {
                b1 = ctr % 2;
                b2 = (ctr >> 1) % 2;
                b3 = (ctr >> 2) % 2;
                b4 = (ctr >> 3) % 2;
                b5 = (ctr >> 4) % 2;
                b6 = (ctr >> 5) % 2;
                b7 = (ctr >> 6) % 2;

                swpIndex = b1 * 64 + b2 * 32 + b3 * 16 + b4 * 8 + b5 * 4 + b6 * 2 + b7;

                if (swpIndex > ctr)
                {
                    u1 = (int)A[2 * ctr];
                    u2 = (int)A[2 * ctr + 1];
                    A[2 * ctr] = A[2 * swpIndex];
                    A[2 * ctr + 1] = A[2 * swpIndex + 1];
                    A[2 * swpIndex] = (uint)u1;
                    A[2 * swpIndex + 1] = (uint)u2;
                }
            }
        }

        private void RLWEDecrypt(uint[] C1, uint[] C2, uint[] R2)
        {
            FFT.Mul2(C1, C1, R2, MODULUS);	        // c1 <-- c1*r2
            FFT.Add2(C1, C1, C2, MODULUS);	        // c1 <-- c1*r2 + c2

            InvNTT(C1);
        }

        private void RLWEEncrypt(uint[] A, uint[] C1, uint[] C2, uint[] Msg, uint[] P)
        {
            uint[] e1 = new uint[M];
            uint[] e2 = new uint[M];
            uint[] e3 = new uint[M];
            uint[] encMsg = new uint[M];

            for (int i = 0; i < M; i++)
                encMsg[i] = Msg[i] * QBY2;              // encoding

            if (ParallelUtils.IsParallel)
            {
                Action[] kA = new Action[] 
                { 
                    new Action(()=> KnuthYao(e1)), 
                    new Action(()=> KnuthYao(e2)), 
                    new Action(()=> KnuthYao(e3))
                };
                Parallel.Invoke(kA);

                FFT.Add2(e3, e3, encMsg, MODULUS);	    // e3 <-- e3 + m

                Action[] nA = new Action[] 
                { 
                    new Action(()=> FwdNTT(e1)), 
                    new Action(()=> FwdNTT(e2)), 
                    new Action(()=> FwdNTT(e3))
                };
                Parallel.Invoke(nA);

                // m <-- a*e1
                FFT.Mul2(C1, A, e1, MODULUS); 	        // c1 <-- a*e1
                FFT.Add2(C1, e2, C1, MODULUS);	        // c1 <-- e2 + a*e1(tmp_m);
                FFT.Mul2(C2, P, e1, MODULUS); 		    // c2 <-- p*e1
                FFT.Add2(C2, e3, C2, MODULUS);	        // c2<-- e3 + p*e1

                Action[] rA = new Action[] 
                { 
                    new Action(()=> Rearrange(C1)), 
                    new Action(()=> Rearrange(C2))
                };
                Parallel.Invoke(rA);
            }
            else
            {
                KnuthYao(e1);
                KnuthYao(e2);
                KnuthYao(e3);

                FFT.Add2(e3, e3, encMsg, MODULUS);	    // e3 <-- e3 + m

                FwdNTT(e1);
                FwdNTT(e2);
                FwdNTT(e3);

                // m <-- a*e1
                FFT.Mul2(C1, A, e1, MODULUS); 	        // c1 <-- a*e1
                FFT.Add2(C1, e2, C1, MODULUS);	        // c1 <-- e2 + a*e1(tmp_m);
                FFT.Mul2(C2, P, e1, MODULUS); 		    // c2 <-- p*e1
                FFT.Add2(C2, e3, C2, MODULUS);	        // c2<-- e3 + p*e1

                Rearrange(C1);
                Rearrange(C2);
            }
        }
        #endregion
    }
}
