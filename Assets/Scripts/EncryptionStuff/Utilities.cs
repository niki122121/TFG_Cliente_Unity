using Microsoft.Research.SEAL;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.CompilerServices;


namespace SealUtilites
{
    public static class Utilities
    {
        /// <summary>
        /// Helper function: Prints the name of the example in a fancy banner.
        /// </summary>
        public static void PrintExampleBanner(string title)
        {
            if (!string.IsNullOrEmpty(title))
            {
                int titleLength = title.Length;
                int bannerLength = titleLength + 2 * 10;
                string bannerTop = "+" + new string('-', bannerLength - 2) + "+";
                string bannerMiddle =
                    "|" + new string(' ', 9) + title + new string(' ', 9) + "|";

                Debug.Log("");
                Debug.Log(bannerTop);
                Debug.Log(bannerMiddle);
                Debug.Log(bannerTop);
            }
        }

        /// <summary>
        /// Helper function: Prints the parameters in a SEALContext.
        /// </summary>
        public static void PrintParameters(SEALContext context)
        {
            // Verify parameters
            if (null == context)
            {
                throw new ArgumentNullException("context is not set");
            }
            SEALContext.ContextData contextData = context.KeyContextData;

            /*
            Which scheme are we using?
            */
            string schemeName = null;
            switch (contextData.Parms.Scheme)
            {
                case SchemeType.BFV:
                    schemeName = "BFV";
                    break;
                case SchemeType.CKKS:
                    schemeName = "CKKS";
                    break;
                default:
                    throw new ArgumentException("unsupported scheme");
            }

            Debug.Log("/");
            Debug.Log("| Encryption parameters:");
            Debug.Log($"|   Scheme: {schemeName}");
            Debug.Log("|   PolyModulusDegree: {0} " +
                contextData.Parms.PolyModulusDegree);

            /*
            Print the size of the true (product) coefficient modulus.
            */
            string printCoef = "|   CoeffModulus size:"+ contextData.TotalCoeffModulusBitCount +"  (";
            List<Modulus> coeffModulus = (List<Modulus>)contextData.Parms.CoeffModulus;
            for (int i = 0; i < coeffModulus.Count - 1; i++)
            {
                printCoef += ($"{coeffModulus[i].BitCount} + ");
            }
            printCoef += coeffModulus.Last().BitCount + ") bits";
            Debug.Log(printCoef);

            /*
            For the BFV scheme print the PlainModulus parameter.
            */
            if (contextData.Parms.Scheme == SchemeType.BFV)
            {
                Debug.Log("|   PlainModulus: {0} " +
                    contextData.Parms.PlainModulus.Value);
            }

            Debug.Log("\\");
        }

        /// <summary>
        /// Helper function: Print the first and last printSize elements
        /// of a 2 row matrix.
        /// </summary>
        public static void PrintMatrix(IEnumerable<ulong> matrixPar,
            int rowSize, int printSize = 30)
        {
            ulong[] matrix = matrixPar.ToArray();
            Debug.Log("");

            /*
            We're not going to print every column of the matrix (may be big). Instead
            print printSize slots from beginning and end of the matrix.
            */
            string matrixPrint = "    [";
            for (int i = 0; i < printSize; i++)
            {
                matrixPrint += (matrix[i].ToString() + ",  ");
            }
            matrixPrint +=  " ...";
            for (int i = rowSize - printSize; i < rowSize; i++)
            {
                matrixPrint += (",  " + matrix[i].ToString());
            }
            matrixPrint += "  ]";
            Debug.Log(matrixPrint);
            matrixPrint = "    [";
            for (int i = rowSize; i < rowSize + printSize; i++)
            {
                matrixPrint += (matrix[i].ToString() + ",  ");
            }
            matrixPrint += " ...";
            for (int i = 2 * rowSize - printSize; i < 2 * rowSize; i++)
            {
                matrixPrint += (",  " + matrix[i].ToString());
            }
            matrixPrint += "  ]";
            Debug.Log(matrixPrint);
            Debug.Log("");
        }

        /// <summary>
        /// Helper function: Convert a ulong to a hex string representation
        /// </summary>
        public static string ULongToString(ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        /// <summary>
        /// Helper function: Prints a vector of floating-point values.
        /// </summary>
        public static void PrintVector<T>(
            IEnumerable<T> vec, int printSize = 4, int prec = 3)
        {
            string numFormat = string.Format("{{0:N{0}}}", prec);
            T[] veca = vec.ToArray();
            int slotCount = veca.Length;
            Debug.Log("");
            if (slotCount <= 2 * printSize)
            {
                Debug.Log("    [");
                for (int i = 0; i < slotCount; i++)
                {
                    Debug.Log(" " + string.Format(numFormat, veca[i]));
                    if (i != (slotCount - 1))
                        Debug.Log(",");
                    else
                        Debug.Log(" ]");
                }
                Debug.Log("");
            }
            else
            {
                Debug.Log("    [");
                for (int i = 0; i < printSize; i++)
                {
                    Debug.Log(" " + string.Format(numFormat, veca[i]) + ", ");
                }
                if (veca.Length > 2 * printSize)
                {
                    Debug.Log(" ...");
                }
                for (int i = slotCount - printSize; i < slotCount; i++)
                {
                    Debug.Log(", " + string.Format(numFormat, veca[i]));
                }
                Debug.Log(" ]");
            }
            Debug.Log("");
        }

        public static void PrintLine([CallerLineNumber] int lineNumber = 0)
        {
            Debug.Log("Line {0,3} -->  " + lineNumber);
        }
    }
}
