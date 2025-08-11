using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EDAProto;
using GlobalDerivativesApplications.EdaProto;
using ExcelDna.Integration;


namespace Interpolation
{
    public enum InterpolationType { E_Constant, E_Step, E_Linear, E_LinearC, E_SplineVol, E_SplineL, E_SplineC, E_Spline, E_SplineSqrLog, E_SplineSqrLogC };

    public class InterpolationStructure
    {
        int m_lN;

        InterpolationType m_eType;
        public void setType(InterpolationType p_eType)
        {
            m_eType = p_eType;
            m_bModif = true;
        }
        public InterpolationType getType()
        {
            return m_eType;
        }

        double m_dConstant;

        double[] m_vX;
        /// <summary>
        /// accesseur à l'axe des abscisses
        /// </summary>
        public double[] VX
        {
            get
            {
                if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
                    return m_vExpX;

                return m_vX;
            }
            set
            {
                m_bModif = true;

                if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
                    m_vExpX = value;
                else
                    m_vX = value;
            }
        }

        /// <summary>
        /// renvoie la ième abscisse
        /// </summary>
        /// <param name="p_lIndexX"></param>
        /// <returns></returns>
        public double getX(int p_lIndexX)
        {
            if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
                return m_vExpX[p_lIndexX];

            return m_vX[p_lIndexX];
        }
        /// <summary>
        /// set la ième abscisse à une valeur
        /// </summary>
        /// <param name="p_lIndexX"></param>
        /// <param name="p_dValue"></param>
        public void setX(int p_lIndexX, double p_dValue)
        {
            m_bModif = true;

            if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
                m_vExpX[p_lIndexX] = Math.Exp(p_dValue);
            else
                m_vX[p_lIndexX] = p_dValue;

        }

        double[] m_vY;
        /// <summary>
        /// accesseur à l'axe des ordonnées
        /// </summary>
        public double[] VY
        {
            get
            {
                if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
                    return m_vSqrtY;

                return m_vY;
            }
            set
            {
                m_bModif = true;

                if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
                    m_vSqrtY = value;
                else
                    m_vY = value;
            }
        }

        double[] m_vExpX, m_vSqrtY; // pour la spline sur le log 

        /// <summary>
        /// renvoie la ième ordonnée
        /// </summary>
        /// <param name="p_lIndexY"></param>
        /// <returns></returns>
        public double getY(int p_lIndexY)
        {
            if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
                return m_vSqrtY[p_lIndexY];

            return m_vY[p_lIndexY];
        }
        /// <summary>
        /// set la ième ordonnée à une valeur
        /// </summary>
        /// <param name="p_lIndexY"></param>
        /// <param name="p_dValue"></param>
        public void setY(int p_lIndexY, double p_dValue)
        {
            m_bModif = true;

            if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
                m_vSqrtY[p_lIndexY] = Math.Sqrt(Math.Max(p_dValue, 0.0));
            else
                m_vY[p_lIndexY] = p_dValue;
        }

        /// <summary>
        /// on shift tous les m_vY parallèlement
        /// </summary>
        /// <param name="p_dValue"></param>
        public void shift(double p_dValue)
        {
            m_bModif = true;

            if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
            {
                for (int l_lIndexY = 0; l_lIndexY < VY.Length; ++l_lIndexY)
                    m_vSqrtY[l_lIndexY] = Math.Sqrt(Math.Max(m_vY[l_lIndexY] + p_dValue, 0.0));
            }
            else
            {
                for (int l_lIndexY = 0; l_lIndexY < VY.Length; ++l_lIndexY)
                    m_vY[l_lIndexY] += p_dValue;
            }
        }

        /// <summary>
        /// renvoie un vecteur y / y(i) = f(x(i)) 
        /// </summary>
        /// <param name="p_vXValue"> vecteur x </param>
        /// <returns></returns>
        public double[] getY(double[] p_vX)
        {
            int l_lIndexI;
            int[] l_vIndex;
            double[] l_vValue;
            double l_dLogX; // que pour les interploation sur le log

            l_vValue = new double[p_vX.Length];

            // cas particuliers simples
            if (m_eType == InterpolationType.E_Constant)
            {
                for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    l_vValue[l_lIndexI] = m_dConstant;
                return l_vValue;
            }

            if (m_lN == 0)
            {
                for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    l_vValue[l_lIndexI] = 0;
                return l_vValue;
            }

            if (m_lN == 1)
            {
                for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    l_vValue[l_lIndexI] = m_vY[0];
                return l_vValue;
            }

            switch (m_eType)
            {
                case InterpolationType.E_Step:

                    l_vIndex = getIndex(m_vX, p_vX);

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        if (l_vIndex[l_lIndexI] == 0)
                        {
                            l_vValue[l_lIndexI] = m_vY[0];
                            continue;
                        }

                        if (l_vIndex[l_lIndexI] == m_lN)
                        {
                            l_vValue[l_lIndexI] = m_vY[m_lN - 1];
                            continue;
                        }

                        // cadlag (par convention)
                        if (p_vX[l_lIndexI] == m_vX[l_vIndex[l_lIndexI]])
                        {
                            l_vValue[l_lIndexI] = m_vY[l_vIndex[l_lIndexI]];
                            continue;
                        }

                        l_vValue[l_lIndexI] = m_vY[l_vIndex[l_lIndexI] - 1];
                    }
                    return l_vValue;

                case InterpolationType.E_Linear:
                    // interpolation + extrapolation linéaire

                    l_vIndex = getIndex(m_vX, p_vX);

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        if (l_vIndex[l_lIndexI] == 0)
                        {
                            l_vValue[l_lIndexI] = (p_vX[l_lIndexI] - m_vX[0]) * ((m_vY[0] - m_vY[1]) / (m_vX[0] - m_vX[1])) + m_vY[0];
                            continue;
                        }
                        if (l_vIndex[l_lIndexI] == m_lN)
                        {
                            l_vValue[l_lIndexI] = (p_vX[l_lIndexI] - m_vX[m_lN - 1]) * ((m_vY[m_lN - 1] - m_vY[m_lN - 2]) / (m_vX[m_lN - 1] - m_vX[m_lN - 2])) + m_vY[m_lN - 1];
                            continue;
                        }

                        l_vValue[l_lIndexI] = (p_vX[l_lIndexI] - m_vX[l_vIndex[l_lIndexI]]) * ((m_vY[l_vIndex[l_lIndexI]] - m_vY[l_vIndex[l_lIndexI] - 1]) / (m_vX[l_vIndex[l_lIndexI]] - m_vX[l_vIndex[l_lIndexI] - 1])) + m_vY[l_vIndex[l_lIndexI]];
                    }
                    return l_vValue;

                case InterpolationType.E_LinearC:
                    // interpolation linéaire + extrapolation constante

                    l_vIndex = getIndex(m_vX, p_vX);

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        if (l_vIndex[l_lIndexI] == 0)
                        {
                            l_vValue[l_lIndexI] = m_vY[0];
                            continue;
                        }

                        if (l_vIndex[l_lIndexI] == m_lN)
                        {
                            l_vValue[l_lIndexI] = m_vY[m_lN - 1];
                            continue;
                        }

                        l_vValue[l_lIndexI] = (p_vX[l_lIndexI] - m_vX[l_vIndex[l_lIndexI]]) * ((m_vY[l_vIndex[l_lIndexI]] - m_vY[l_vIndex[l_lIndexI] - 1]) / (m_vX[l_vIndex[l_lIndexI]] - m_vX[l_vIndex[l_lIndexI] - 1])) + m_vY[l_vIndex[l_lIndexI]];
                    }
                    return l_vValue;


                case InterpolationType.E_Spline:

                    if (m_bModif == true)
                    {
                        buildDerivatives();
                        m_bModif = false;
                    }

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        l_vValue[l_lIndexI] = splint(p_vX[l_lIndexI]);
                    }
                    return l_vValue;

                case InterpolationType.E_SplineVol:

                    if (m_bModif == true)
                    {
                        buildDerivatives();
                        m_bModif = false;
                    }

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        if (p_vX[l_lIndexI] < m_vX[0])
                        {
                            if (m_vSplineDer[0] <= 0) // on extrapole linéairement en vol²                                 
                                l_vValue[l_lIndexI] = Math.Sqrt(m_vY[0] * (m_vY[0] + 2.0 * m_vSplineDer[0] * (p_vX[l_lIndexI] - m_vX[0])));
                            else // on extrapole en tangente hyperbolique
                                l_vValue[l_lIndexI] = m_vY[0] * (Math.Tanh((m_vSplineDer[0] / m_vY[0]) * (p_vX[l_lIndexI] - m_vX[0])) + 1);
                            continue;
                        }

                        if (p_vX[l_lIndexI] > m_vX[m_lN - 1])
                        {
                            if (m_vSplineDer[m_lN - 1] >= 0) // on extrapole linéairement en vol²    
                                l_vValue[l_lIndexI] = Math.Sqrt(m_vY[m_lN - 1] * (m_vY[m_lN - 1] + 2.0 * m_vSplineDer[m_lN - 1] * (p_vX[l_lIndexI] - m_vX[m_lN - 1])));
                            else // on extrapole en tangente hyperbolique
                                l_vValue[l_lIndexI] = -m_vY[m_lN - 1] * (Math.Tanh(-(m_vSplineDer[m_lN - 1] / m_vY[m_lN - 1]) * (p_vX[l_lIndexI] - m_vX[m_lN - 1])) - 1);
                            continue;
                        }

                        l_vValue[l_lIndexI] = splint(p_vX[l_lIndexI]);
                    }
                    return l_vValue;

                case InterpolationType.E_SplineC:

                    if (m_bModif == true)
                    {
                        buildDerivatives();
                        m_bModif = false;
                    }

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        if (p_vX[l_lIndexI] <= m_vX[0])
                        {
                            l_vValue[l_lIndexI] = m_vY[0];
                            continue;
                        }

                        if (p_vX[l_lIndexI] > m_vX[m_lN - 1])
                        {
                            l_vValue[l_lIndexI] = m_vY[m_lN - 1];
                            continue;
                        }

                        l_vValue[l_lIndexI] = splint(p_vX[l_lIndexI]);
                    }
                    return l_vValue;
                //Interpolation spline et Extrapolation Lineaire.
                case InterpolationType.E_SplineL:

                    if (m_bModif == true)
                    {
                        buildDerivatives();
                        m_bModif = false;
                    }

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        if (p_vX[l_lIndexI] <= m_vX[0])
                        {
                            l_vValue[l_lIndexI] = m_vY[0] + m_vSplineDer[0] * (p_vX[l_lIndexI] - m_vX[0]);
                            continue;
                        }

                        if (p_vX[l_lIndexI] > m_vX[m_lN - 1])
                        {
                            l_vValue[l_lIndexI] = m_vY[m_lN - 1] + m_vSplineDer[m_lN - 1] * (p_vX[l_lIndexI] - m_vX[m_lN - 1]);
                            continue;
                        }

                        l_vValue[l_lIndexI] = splint(p_vX[l_lIndexI]);
                    }
                    return l_vValue;
                case InterpolationType.E_SplineSqrLog:

                    if (m_bModif == true)
                    {
                        m_vExpX = CToolsArray.getExp(m_vX);
                        m_vSqrtY = CToolsArray.getSqrt(m_vY);
                        buildDerivatives();
                        m_bModif = false;
                    }

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        l_dLogX = Math.Log(p_vX[l_lIndexI]);

                        if (l_dLogX < m_vX[0])
                        {
                            if (m_vSplineDer[0] <= 0) // on extrapole linéairement
                                l_vValue[l_lIndexI] = m_vY[0] + m_vSplineDer[0] * (l_dLogX - m_vX[0]);
                            else // on extrapole en tangente hyperbolique
                                l_vValue[l_lIndexI] = m_vY[0] * (Math.Tanh((m_vSplineDer[0] / m_vY[0]) * (l_dLogX - m_vX[0])) + 1);

                            l_vValue[l_lIndexI] = Math.Sqrt(Math.Max(l_vValue[l_lIndexI], 0.0));
                            continue;
                        }

                        if (l_dLogX > m_vX[m_lN - 1])
                        {
                            if (m_vSplineDer[m_lN - 1] >= 0) // on extrapole linéairement
                                l_vValue[l_lIndexI] = m_vY[m_lN - 1] + m_vSplineDer[m_lN - 1] * (l_dLogX - m_vX[m_lN - 1]);
                            else // on extrapole en tangente hyperbolique
                                l_vValue[l_lIndexI] = -m_vY[m_lN - 1] * (Math.Tanh(-(m_vSplineDer[m_lN - 1] / m_vY[m_lN - 1]) * (l_dLogX - m_vX[m_lN - 1])) - 1);

                            l_vValue[l_lIndexI] = Math.Sqrt(Math.Max(l_vValue[l_lIndexI], 0.0));
                            continue;
                        }

                        l_vValue[l_lIndexI] = Math.Sqrt(Math.Max(splint(l_dLogX), 0.0));
                    }
                    return l_vValue;

                case InterpolationType.E_SplineSqrLogC:

                    if (m_bModif == true)
                    {
                        m_vExpX = CToolsArray.getExp(m_vX);
                        m_vSqrtY = CToolsArray.getSqrt(m_vY);
                        buildDerivatives();
                        m_bModif = false;
                    }

                    for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                    {
                        l_dLogX = Math.Log(p_vX[l_lIndexI]);

                        if (l_dLogX <= m_vX[0])
                        {
                            l_vValue[l_lIndexI] = Math.Sqrt(Math.Max(m_vY[0], 0.0));
                            continue;
                        }

                        if (l_dLogX > m_vX[m_lN - 1])
                        {
                            l_vValue[l_lIndexI] = Math.Sqrt(Math.Max(m_vY[m_lN - 1], 0.0));
                            continue;
                        }

                        l_vValue[l_lIndexI] = Math.Sqrt(Math.Max(splint(l_dLogX), 0.0));
                    }
                    return l_vValue;

            }
            return null;
        }
        /// <summary>
        /// renvoie y = f(x)
        /// </summary>
        /// <param name="p_dX"> valeur x </param>
        /// <returns></returns>
        public double getY(double p_dX)
        {
            return getY(new double[] { p_dX })[0];
        }

        double[] m_vSlope;
        /// <summary>
        /// défini la ième pente à une valeur
        /// </summary>
        /// <param name="p_lIndex"></param>
        /// <param name="p_dValue"></param>
        public void setSlope(int p_lIndex, double p_dValue)
        {
            m_vSlope[p_lIndex] = p_dValue;
        }
        /// <summary>
        /// renvoie la ième pente
        /// </summary>
        /// <param name="p_lIndex"></param>
        /// <returns></returns>
        public double getSlope(int p_lIndex)
        {
            return m_vSlope[p_lIndex];
        }

        bool m_bModif;

        // spline parameters
        double[] m_vSplineDer;
        double[] m_vSplineDer2;

        public InterpolationStructure() { }
        public InterpolationStructure(double p_dConstant)
        {
            m_dConstant = p_dConstant;
            m_eType = InterpolationType.E_Constant;
        }

        /// <summary>
        /// constructeur principal
        /// </summary>
        /// <param name="p_vX"></param>
        /// <param name="p_vY"></param>
        /// <param name="p_eType"></param>
        public InterpolationStructure(double[] p_vX, double[] p_vY, InterpolationType p_eType)
        {
            build(p_vX, p_vY, p_eType);
        }
        /// <summary>
        /// constructeur où les valeurs {X,Y} sont passées par tableau
        /// </summary>
        /// <param name="p_vValue"></param>
        /// <param name="p_eType"></param>
        public InterpolationStructure(double[,] p_vValue, InterpolationType p_eType)
        {
            int l_lIndexX;
            double[] l_vX, l_vY;

            l_vX = new double[p_vValue.GetLength(0)];
            l_vY = new double[p_vValue.GetLength(0)];
            for (l_lIndexX = 0; l_lIndexX < p_vValue.GetLength(0); l_lIndexX++)
            {
                l_vX[l_lIndexX] = p_vValue[l_lIndexX, 0];
                l_vY[l_lIndexX] = p_vValue[l_lIndexX, 1];
            }
            build(l_vX, l_vY, p_eType);
        }
        /// <summary>
        /// coeur de construction
        /// </summary>
        /// <param name="p_vX"></param>
        /// <param name="p_vY"></param>
        /// <param name="p_eType"></param>
        private void build(double[] p_vX, double[] p_vY, InterpolationType p_eType)
        {
            bool l_bIsLog = false;

            m_lN = p_vX.Length;

            if (p_eType == InterpolationType.E_SplineSqrLog || p_eType == InterpolationType.E_SplineSqrLogC)
            {
                m_vExpX = p_vX;
                m_vSqrtY = p_vY == null ? new double[m_lN] : p_vY;

                m_vX = CToolsArray.getLn(p_vX);
                m_vY = CToolsArray.getProduct(p_vY, p_vY);

                l_bIsLog = true;
            }
            else
            {
                m_vX = p_vX;
                m_vY = p_vY == null ? new double[m_lN] : p_vY;
            }

            m_vSlope = new double[m_lN];

            m_eType = p_eType;

            if (p_eType == InterpolationType.E_SplineVol || p_eType == InterpolationType.E_SplineC || p_eType == InterpolationType.E_SplineL || l_bIsLog || p_eType == InterpolationType.E_Spline)
            {
                m_vSplineDer = new double[m_lN];
                m_vSplineDer2 = new double[m_lN];
                buildDerivatives();
            }

            m_bModif = false;
        }

        /// <summary>
        /// constructeur de recopie
        /// </summary>
        /// <param name="p_oFunc"></param>      
        public InterpolationStructure(InterpolationStructure p_oFunc)
        {
            if (p_oFunc == null)
            {
                m_lN = 0;
                return;
            }

            int l_lIndex, l_lSize;

            m_lN = p_oFunc.m_lN;

            m_vX = new double[m_lN];
            m_vY = new double[m_lN];
            for (l_lIndex = 0; l_lIndex < m_lN; l_lIndex++)
            {
                m_vX[l_lIndex] = p_oFunc.m_vX[l_lIndex];
                m_vY[l_lIndex] = p_oFunc.m_vY[l_lIndex];
            }

            m_dConstant = p_oFunc.m_dConstant;
            m_eType = p_oFunc.m_eType;

            if (p_oFunc.m_vSlope != null)
            {
                l_lSize = p_oFunc.m_vSlope.Length;
                m_vSlope = new double[l_lSize];
                for (l_lIndex = 0; l_lIndex < l_lSize; l_lIndex++)
                    m_vSlope[l_lIndex] = p_oFunc.m_vSlope[l_lIndex];
            }

            if (p_oFunc.m_vSplineDer != null)
            {
                l_lSize = p_oFunc.m_vSplineDer.Length;
                m_vSplineDer = new double[l_lSize];
                for (l_lIndex = 0; l_lIndex < l_lSize; l_lIndex++)
                    m_vSplineDer[l_lIndex] = p_oFunc.m_vSplineDer[l_lIndex];
            }

            if (p_oFunc.m_vSplineDer2 != null)
            {
                l_lSize = p_oFunc.m_vSplineDer2.Length;
                m_vSplineDer2 = new double[l_lSize];
                for (l_lIndex = 0; l_lIndex < l_lSize; l_lIndex++)
                    m_vSplineDer2[l_lIndex] = p_oFunc.m_vSplineDer2[l_lIndex];
            }

            if (m_eType == InterpolationType.E_SplineSqrLog || m_eType == InterpolationType.E_SplineSqrLogC)
            {
                m_vExpX = CToolsArray.getExp(m_vX);
                m_vSqrtY = CToolsArray.getSqrt(m_vY);
            }
            m_bModif = false;
        }

        public int getSize()
        {
            return m_lN;
        }

        int[] getIndex(double[] p_vValue)
        {
            return getIndex(m_vX, p_vValue);
        }
        int getIndex(double p_dValue)
        {
            double[] l_vValue;
            int[] l_vIndex;

            l_vValue = new double[1];
            l_vValue[0] = p_dValue;
            l_vIndex = getIndex(l_vValue);

            return l_vIndex[0];
        }

        //fct vectorielle. Value est un vecteur de valeur croissante. On cherche les indices i(j) dans X tels que X[i-1] < Value[j] <= X[i]. En temps normal, 1<=i(j)<=n-1. Si Value[j]<=X[0] on renvoit i(j)=0. Si Value[j]>X[n-1] on renvoit i(j)=n  
        static public int[] getIndexValueSort(double[] p_vX, double[] p_vValue)
        {
            int l_lIndexJ, l_lIndexI, l_lBorneInf, l_lBorneSup, l_lIndexInf, l_lIndexMid, l_lIndexSup;
            int[] l_vIndex;
            l_vIndex = new int[p_vValue.Length];

            l_lBorneInf = 0;
            l_lBorneSup = p_vX.Length - 1;

            l_lIndexJ = p_vValue.Length;
            for (l_lIndexI = 0; l_lIndexI < p_vValue.Length; l_lIndexI++)
            {
                // les Target[j] sont balayées dans l'ordre 0,n-1,1,n-2 etc...
                l_lIndexJ = p_vValue.Length - 1 - l_lIndexJ + (l_lIndexI % 2 == 0 ? 1 : 0);

                if (p_vX[0] >= p_vValue[l_lIndexJ])
                    l_vIndex[l_lIndexJ] = 0;
                else
                    if (p_vX[p_vX.Length - 1] < p_vValue[l_lIndexJ])
                        l_vIndex[l_lIndexJ] = p_vX.Length;
                    else
                    // dichotomie : on cherche i / X[i-1] < Target[j] <= X[i]
                    {
                        l_lIndexInf = l_lBorneInf;
                        l_lIndexSup = l_lBorneSup;
                        l_lIndexMid = (int)((l_lIndexSup + l_lIndexInf) / 2);
                        while (l_lIndexSup - l_lIndexInf > 1)
                        {
                            if (p_vValue[l_lIndexJ] - p_vX[l_lIndexInf] > 0 && p_vValue[l_lIndexJ] - p_vX[l_lIndexMid] <= 0)
                            {	// i appartient à ]inf,mid]
                                l_lIndexSup = l_lIndexMid;
                                l_lIndexMid = (int)((l_lIndexInf + l_lIndexMid) / 2);
                            }
                            else
                            {	// i appartient à ]mid,max]
                                l_lIndexInf = l_lIndexMid;
                                l_lIndexMid = (int)((l_lIndexSup + l_lIndexMid) / 2);
                            }
                        }

                        l_vIndex[l_lIndexJ] = l_lIndexSup;

                        // on resserre les bornes en fonctions des solutions précédentes
                        if (l_lIndexI % 2 == 0)
                            l_lBorneInf = l_lIndexInf;
                        if (l_lIndexI % 2 == 1)
                            l_lBorneSup = l_lIndexSup;
                    }
            }
            return l_vIndex;
        }

        //fct vectorielle. Value est un vecteur de valeur non ordonnées. On cherche les indices i(j) dans X tels que X[i-1] < Value[j] <= X[i]. En temps normal, 1<=i(j)<=n-1. Si Value[j]<=X[0] on renvoit i(j)=0. Si Value[j]>X[n-1] on renvoit i(j)=n  
        static public int[] getIndex(double[] p_vX, double[] p_vValue)
        {
            int l_lIndexI;
            int[] l_vIndex, l_vIndexSort, l_vOrd;
            double[] l_vXValueSort;
            bool l_bIsValueSort;

            // on regarde si les valeurs sont déjà classées en ordre croissant
            l_lIndexI = 0;
            while (l_lIndexI < p_vValue.Length - 1 && p_vValue[l_lIndexI + 1] >= p_vValue[l_lIndexI])
            {
                l_lIndexI++;
            }
            l_bIsValueSort = l_lIndexI == p_vValue.Length - 1 ? true : false;

            if (l_bIsValueSort == false) // si non, on les classe puis on appelle getIndexValueSort
            {
                l_vXValueSort = new double[p_vValue.Length];
                for (l_lIndexI = 0; l_lIndexI < p_vValue.Length; l_lIndexI++)
                    l_vXValueSort[l_lIndexI] = p_vValue[l_lIndexI];

                l_vOrd = new int[p_vValue.Length];

                CNumericalFunction.sort(l_vXValueSort, l_vOrd);

                // appel à getIndexValueSort une fois les valeurs ordonnées
                l_vIndexSort = getIndexValueSort(p_vX, l_vXValueSort);

                // construction du vecteur d'indice dans l'ordre inital
                l_vIndex = new int[p_vValue.Length];
                for (l_lIndexI = 0; l_lIndexI < p_vValue.Length; l_lIndexI++)
                    l_vIndex[l_vOrd[l_lIndexI]] = l_vIndexSort[l_lIndexI];
            }
            else // si oui, on appelle getIndexValueSort directement
                l_vIndex = getIndexValueSort(p_vX, p_vValue);

            return l_vIndex;
        }

        /// <summary>
        /// renvoie true si fonction nulle, false sinon
        /// </summary>
        /// <returns></returns>
        public bool IsZero()
        {
            int l_lIndex;

            for (l_lIndex = 0; l_lIndex < m_lN; l_lIndex++)
                if (m_vY[l_lIndex] != 0.0)
                    return false;

            return true;
        }

        // renvoie l'intégrale de la fonction entre X[0] et X
        public double getSum(double p_dX)
        {
            if (m_lN == 0)
                return 0;

            int l_lIndexI;
            double l_dSum, l_dX, l_dAbs, l_dOrd, l_dDT1, l_dDT2, l_dX1, l_dX2, l_dY1, l_dY2, l_dYxx1, l_dYxx2, l_dA, l_dB, l_dSumA, l_dSumB, l_dSumA3, l_dSumB3, l_dSumI;

            l_dSum = 0;
            switch (m_eType)
            {
                case InterpolationType.E_Constant:
                    l_dSum = m_vY[0] * (p_dX - m_vX[0]);
                    return l_dSum;

                case InterpolationType.E_Step:
                    l_lIndexI = 1;
                    while (l_lIndexI < m_lN && m_vX[l_lIndexI] < p_dX)
                    {
                        l_dSum += m_vY[l_lIndexI - 1] * (m_vX[l_lIndexI] - m_vX[l_lIndexI - 1]);
                        l_lIndexI++;
                    }
                    l_dSum += m_vY[l_lIndexI - 1] * (p_dX - m_vX[l_lIndexI - 1]);
                    return l_dSum;

                case InterpolationType.E_Linear:
                    l_dSum = 0;
                    l_lIndexI = 1;
                    while (l_lIndexI < m_lN && m_vX[l_lIndexI] <= p_dX)
                    {
                        l_dX = m_vX[l_lIndexI] - m_vX[l_lIndexI - 1];
                        l_dDT1 = m_vX[l_lIndexI - 1] - m_vX[0];
                        l_dDT2 = m_vX[l_lIndexI] - m_vX[0];

                        l_dAbs = (m_vY[l_lIndexI] - m_vY[l_lIndexI - 1]) / l_dX;
                        l_dOrd = m_vY[l_lIndexI] - l_dDT2 * l_dAbs;

                        l_dSum += 0.5 * l_dAbs * (l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1) + l_dOrd * (l_dDT2 - l_dDT1);
                        l_lIndexI++;
                    }
                    if (l_lIndexI < m_lN && m_vX[l_lIndexI] > p_dX)
                    {
                        l_dX = m_vX[l_lIndexI] - m_vX[l_lIndexI - 1];
                        l_dDT1 = m_vX[l_lIndexI - 1] - m_vX[0];
                        l_dDT2 = p_dX - m_vX[0];

                        l_dAbs = (m_vY[l_lIndexI] - m_vY[l_lIndexI - 1]) / l_dX;
                        l_dOrd = m_vY[l_lIndexI - 1] - l_dDT1 * l_dAbs;

                        l_dSum += 0.5 * l_dAbs * (l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1) + l_dOrd * (l_dDT2 - l_dDT1);
                    }
                    if (l_lIndexI > m_lN)
                    {
                        l_dX = m_vX[m_lN - 1] - m_vX[m_lN - 2];
                        l_dDT1 = m_vX[m_lN - 1] - m_vX[0];
                        l_dDT2 = p_dX - m_vX[0];

                        l_dAbs = (m_vY[m_lN - 1] - m_vY[m_lN - 2]) / l_dX;
                        l_dOrd = m_vY[m_lN - 1] - l_dDT1 * l_dAbs;

                        l_dSum += 0.5 * l_dAbs * (l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1) + l_dOrd * (l_dDT2 - l_dDT1);
                    }
                    return l_dSum;

                case InterpolationType.E_SplineC:
                    if (m_bModif == true)
                    {
                        buildDerivatives();
                        m_bModif = false;
                    }

                    if (p_dX <= m_vX[0])
                    {
                        l_dSum = m_vY[0] * (p_dX - m_vX[0]);
                        return l_dSum;
                    }

                    l_dSum = 0;
                    l_lIndexI = 1;
                    //while (l_lIndexI < m_lN && m_vX[l_lIndexI] <= p_dT)
                    while (l_lIndexI < m_lN && m_vX[l_lIndexI - 1] <= p_dX)
                    {
                        l_dX1 = m_vX[l_lIndexI - 1];
                        l_dX2 = m_vX[l_lIndexI];
                        l_dX = l_dX2 <= p_dX ? l_dX2 : p_dX;

                        l_dY1 = m_vY[l_lIndexI - 1];
                        l_dY2 = m_vY[l_lIndexI];

                        l_dYxx1 = m_vSplineDer2[l_lIndexI - 1];
                        l_dYxx2 = m_vSplineDer2[l_lIndexI];

                        l_dA = (l_dX2 - l_dX) / (l_dX2 - l_dX1);
                        l_dB = 1.0 - l_dA;

                        l_dSumA = 0.5 * (l_dX2 - l_dX1) * (1.0 - l_dA * l_dA);
                        l_dSumB = 0.5 * (l_dX2 - l_dX1) * l_dB * l_dB;

                        l_dSumA3 = 0.25 * (l_dX2 - l_dX1) * (1.0 - l_dA * l_dA * l_dA * l_dA);
                        l_dSumB3 = 0.25 * (l_dX2 - l_dX1) * l_dB * l_dB * l_dB * l_dB;

                        l_dSumI = l_dY1 * l_dSumA + l_dY2 * l_dSumB + 1.0 / 6.0 * l_dYxx1 * (l_dX2 - l_dX1) * (l_dX2 - l_dX1) * (l_dSumA3 - l_dSumA) + 1.0 / 6.0 * l_dYxx2 * (l_dX2 - l_dX1) * (l_dX2 - l_dX1) * (l_dSumB3 - l_dSumB);
                        l_dSum += l_dSumI;
                        l_lIndexI++;
                    }
                    if (l_lIndexI == m_lN && m_vX[l_lIndexI - 1] <= p_dX)
                    {
                        l_dSumI = m_vY[m_lN - 1] * (p_dX - m_vX[m_lN - 1]);
                        l_dSum += l_dSumI;
                    }
                    return l_dSum;
            }
            return 0.0;
        }
        public double getSum(double p_dX1, double p_dX2)
        {
            return getSum(p_dX2) - getSum(p_dX1);
        }
        public double[] getSum(double[] p_vX)
        {
            int l_lIndexI;
            double[] l_vSum;

            l_vSum = new double[p_vX.Length];
            for (l_lIndexI = 0; l_lIndexI < p_vX.Length; l_lIndexI++)
                l_vSum[l_lIndexI] = getSum(p_vX[l_lIndexI]);

            return l_vSum;

        }
        public double[] getSum(double p_dX1, double[] p_vX2)
        {
            int l_lIndexSum;
            double l_dSumX1;
            double[] l_vSumX2, l_vSum;

            l_dSumX1 = getSum(p_dX1);
            l_vSumX2 = getSum(p_vX2);
            l_vSum = new double[p_vX2.Length];

            for (l_lIndexSum = 0; l_lIndexSum < p_vX2.Length; l_lIndexSum++)
                l_vSum[l_lIndexSum] = l_vSumX2[l_lIndexSum] - l_dSumX1;

            return l_vSum;
        }

        // INTEGRALE VECTORIELLE OPTIMISEE, A FAIRE PROPREMENT...
        public double[] getSumOptimize(double[] p_vXValue)
        {
            int l_lIndexI, l_lIndexJ, l_lIndexXMax;
            double l_dSum, l_dSumI;
            int[] l_vIndexX;
            double[] l_vSum;

            l_vSum = new double[p_vXValue.Length];
            l_vIndexX = getIndex(p_vXValue);
            l_dSum = 0;
            l_dSumI = 0;
            switch (m_eType)
            {
                case InterpolationType.E_SplineC:

                    if (m_bModif == true)
                    {
                        buildDerivatives();
                        m_bModif = false;
                    }

                    double l_dX = 0.0, l_dX1, l_dX2 = 0.0, l_dY1, l_dY2, l_dYxx1, l_dYxx2, l_dA, l_dB, l_dSumA, l_dSumB, l_dSumA3, l_dSumB3, l_dSumNext;

                    l_dSum = 0;
                    l_dSumNext = 0;
                    l_lIndexI = 1;
                    for (l_lIndexJ = 0; l_lIndexJ < p_vXValue.Length; l_lIndexJ++)
                    {
                        //Value(j) <= X[0]
                        if (l_vIndexX[l_lIndexJ] == 0)
                        {
                            l_dSumI = m_vY[0] * (p_vXValue[l_lIndexJ] - m_vX[0]);
                            l_dSum += l_dSumI;
                        }
                        if (l_vIndexX[l_lIndexJ] > 0)
                        {
                            l_lIndexXMax = (l_vIndexX[l_lIndexJ] == m_lN ? l_vIndexX[l_lIndexJ] - 1 : l_vIndexX[l_lIndexJ]);
                            while (l_lIndexI <= l_lIndexXMax)
                            {
                                l_dX1 = m_vX[l_lIndexI - 1];
                                l_dX2 = m_vX[l_lIndexI];
                                l_dX = (l_lIndexI < l_lIndexXMax || l_vIndexX[l_lIndexJ] == m_lN ? l_dX2 : p_vXValue[l_lIndexJ]);

                                l_dY1 = m_vY[l_lIndexI - 1];
                                l_dY2 = m_vY[l_lIndexI];

                                l_dYxx1 = m_vSplineDer2[l_lIndexI - 1];
                                l_dYxx2 = m_vSplineDer2[l_lIndexI];

                                l_dA = (l_dX2 - l_dX) / (l_dX2 - l_dX1);
                                l_dB = 1.0 - l_dA;

                                l_dSumA = 0.5 * (l_dX2 - l_dX1) * (1.0 - l_dA * l_dA);
                                l_dSumB = 0.5 * (l_dX2 - l_dX1) * l_dB * l_dB;

                                l_dSumA3 = 0.25 * (l_dX2 - l_dX1) * (1.0 + l_dA * l_dA * l_dA * l_dA);
                                l_dSumB3 = 0.25 * (l_dX2 - l_dX1) * l_dB * l_dB * l_dB * l_dB;

                                l_dSumI = l_dY1 * l_dSumA + l_dY2 * l_dSumB + 1.0 / 6.0 * l_dYxx1 * (l_dX2 - l_dX1) * (l_dX2 - l_dX1) * (l_dSumA3 - l_dSumA) + 1.0 / 6.0 * l_dYxx2 * (l_dX2 - l_dX1) * (l_dX2 - l_dX1) * (l_dSumB3 - l_dSumB);
                                l_dSum += l_dSumI;
                                l_lIndexI++;
                            }
                            if (l_dX < l_dX2)
                            {
                                // on enlève le dernier morceau à la somme cumulée
                                l_dSumNext = l_dSum - l_dSumI;
                                l_lIndexI--;
                            }
                            else
                            {
                                l_dSumNext = l_dSum;
                            }
                        }
                        //Value(j) > X[N-1]
                        if (l_vIndexX[l_lIndexJ] == m_lN)
                        {
                            l_dSumI = m_vY[m_lN - 1] * (p_vXValue[l_lIndexJ] - m_vX[m_lN - 1]);
                            l_dSum += l_dSumI;
                        }

                        l_vSum[l_lIndexJ] = l_dSum;
                        l_dSum = l_dSumNext;
                    }
                    break;
            }

            return l_vSum;
        }

        // renvoie l'intégrale de la fonction au carré entre X[0] et T (si T > X[0]) 
        public double getSumSquare(double p_dX)
        {
            if (m_lN == 0)
                return 0;

            int l_lIndexI;
            double l_dRes, l_dPeriod, l_dAbs, l_dOrd, l_dDT1, l_dDT2;

            l_dRes = 0;
            switch (m_eType)
            {
                case InterpolationType.E_Constant:
                    l_dRes = m_vY[0] * m_vY[0] * (p_dX - m_vX[0]);
                    break;

                case InterpolationType.E_Step:
                    l_lIndexI = 1;
                    while (l_lIndexI < m_lN && m_vX[l_lIndexI] < p_dX)
                    {
                        l_dRes += m_vY[l_lIndexI - 1] * m_vY[l_lIndexI - 1] * (m_vX[l_lIndexI] - m_vX[l_lIndexI - 1]);
                        l_lIndexI++;
                    }
                    l_dRes += m_vY[l_lIndexI - 1] * m_vY[l_lIndexI - 1] * (p_dX - m_vX[l_lIndexI - 1]);
                    break;

                case InterpolationType.E_Linear:
                    l_dRes = 0;
                    l_lIndexI = 1;
                    while (l_lIndexI < m_lN && m_vX[l_lIndexI] <= p_dX)
                    {
                        l_dPeriod = m_vX[l_lIndexI] - m_vX[l_lIndexI - 1];
                        l_dDT1 = m_vX[l_lIndexI - 1] - m_vX[0];
                        l_dDT2 = m_vX[l_lIndexI] - m_vX[0];

                        l_dAbs = (m_vY[l_lIndexI] - m_vY[l_lIndexI - 1]) / l_dPeriod;
                        l_dOrd = m_vY[l_lIndexI] - l_dDT2 * l_dAbs;

                        l_dRes += 1 / 3 * l_dAbs * l_dAbs * (l_dDT2 * l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1 * l_dDT1) + l_dAbs * l_dOrd * (l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1) + l_dOrd * l_dOrd * (l_dDT2 - l_dDT1);
                        l_lIndexI++;
                    }
                    if (l_lIndexI < m_lN && m_vX[l_lIndexI] > p_dX)
                    {
                        l_dPeriod = m_vX[l_lIndexI] - m_vX[l_lIndexI - 1];
                        l_dDT1 = m_vX[l_lIndexI - 1] - m_vX[0];
                        l_dDT2 = p_dX - m_vX[0];

                        l_dAbs = (m_vY[l_lIndexI] - m_vY[l_lIndexI - 1]) / l_dPeriod;
                        l_dOrd = m_vY[l_lIndexI - 1] - l_dDT1 * l_dAbs;

                        l_dRes += 1 / 3 * l_dAbs * l_dAbs * (l_dDT2 * l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1 * l_dDT1) + l_dAbs * l_dOrd * (l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1) + l_dOrd * l_dOrd * (l_dDT2 - l_dDT1);
                    }
                    if (l_lIndexI > m_lN)
                    {
                        l_dPeriod = m_vX[m_lN - 1] - m_vX[m_lN - 2];
                        l_dDT1 = m_vX[m_lN - 1] - m_vX[0];
                        l_dDT2 = p_dX - m_vX[0];

                        l_dAbs = (m_vY[m_lN - 1] - m_vY[m_lN - 2]) / l_dPeriod;
                        l_dOrd = m_vY[m_lN - 1] - l_dDT1 * l_dAbs;

                        l_dRes += 1 / 3 * l_dAbs * l_dAbs * (l_dDT2 * l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1 * l_dDT1) + l_dAbs * l_dOrd * (l_dDT2 * l_dDT2 - l_dDT1 * l_dDT1) + l_dOrd * l_dOrd * (l_dDT2 - l_dDT1);
                    }
                    break;
            }

            return l_dRes;
        }
        public double getSumSquare(double p_dX1, double p_dX2)
        {
            return getSumSquare(p_dX2) - getSumSquare(p_dX1);
        }

        // spline
        void spline(double p_dYp1, double p_dYpn)
        {
            spline(m_vX, m_vY, m_lN, p_dYp1, p_dYpn, m_vSplineDer2);
        }
        double splint(double p_dX)
        {
            return splint(m_vX, m_vY, m_vSplineDer2, m_lN, p_dX);
        }
        double splintDerivative(double p_dX)
        {
            return splintDerivative(m_vX, m_vY, m_vSplineDer2, m_lN, p_dX);
        }
        void buildDerivatives()
        {
            if (m_lN == 1)
                return;
            // on construit les dérivées secondes aux points 1 à n (avec les conditions y''(1)=0 et y''(n)=0)
            spline(1e30, 1e30);
            //spline((m_vY[1] - m_vY[0]) / (m_vX[1] - m_vX[0]), (m_vY[m_lN - 1] - m_vY[m_lN - 2]) / (m_vX[m_lN - 1] - m_vX[m_lN - 2]));
            // on calcule (pour la forme) les dérivées premières en utilisant une relation inhérente à la méthode spline cubique
            int l_lIndexI;

            for (l_lIndexI = 0; l_lIndexI < m_lN - 1; l_lIndexI++)
            {
                m_vSplineDer[l_lIndexI] = (m_vY[l_lIndexI + 1] - m_vY[l_lIndexI]) / (m_vX[l_lIndexI + 1] - m_vX[l_lIndexI]) - (1.0 / 3.0) * (m_vX[l_lIndexI + 1] - m_vX[l_lIndexI]) * m_vSplineDer2[l_lIndexI] - 1.0 / 6.0 * (m_vX[l_lIndexI + 1] - m_vX[l_lIndexI]) * m_vSplineDer2[l_lIndexI + 1];
                m_vSplineDer[l_lIndexI + 1] = (m_vY[l_lIndexI + 1] - m_vY[l_lIndexI]) / (m_vX[l_lIndexI + 1] - m_vX[l_lIndexI]) + (1.0 / 6.0) * (m_vX[l_lIndexI + 1] - m_vX[l_lIndexI]) * m_vSplineDer2[l_lIndexI] + 1.0 / 3.0 * (m_vX[l_lIndexI + 1] - m_vX[l_lIndexI]) * m_vSplineDer2[l_lIndexI + 1];
            }
        }

        // TODO

        // Numerical Recipes. 
        // fonction qui calcule les dérivées secondes en chaque point -> y2
        // deux types de conditions limites sont possibles :
        // - soit on se donne des dérivées premières aux points 1 et n (yp1 et ypn)
        // - soit on dit que les dérivées secondes aux points 1 et n sont nulles (on met yp1=ypn=1e30)*/
        static public void spline(double[] p_vX, double[] p_vY, int p_lN, double p_dYp1, double p_dYpn, double[] p_vY2)
        {
            int l_lIndexI, l_lIndexK;
            double l_dP, l_dQn, l_dSig, l_dUn;
            double[] l_vU;

            l_vU = new double[p_lN - 1];

            if (p_dYp1 == 1e30)
                p_vY2[0] = l_vU[0] = 0;
            else
            {
                p_vY2[0] = -0.5;
                l_vU[0] = (3 / (p_vX[1] - p_vX[0])) * ((p_vY[1] - p_vY[0]) / (p_vX[1] - p_vX[0]) - p_dYp1);
            }

            for (l_lIndexI = 1; l_lIndexI < p_lN - 1; l_lIndexI++)
            {
                l_dSig = (p_vX[l_lIndexI] - p_vX[l_lIndexI - 1]) / (p_vX[l_lIndexI + 1] - p_vX[l_lIndexI - 1]);
                l_dP = l_dSig * p_vY2[l_lIndexI - 1] + 2;
                p_vY2[l_lIndexI] = (l_dSig - 1) / l_dP;
                l_vU[l_lIndexI] = (p_vY[l_lIndexI + 1] - p_vY[l_lIndexI]) / (p_vX[l_lIndexI + 1] - p_vX[l_lIndexI]) - (p_vY[l_lIndexI] - p_vY[l_lIndexI - 1]) / (p_vX[l_lIndexI] - p_vX[l_lIndexI - 1]);
                l_vU[l_lIndexI] = (6 * l_vU[l_lIndexI] / (p_vX[l_lIndexI + 1] - p_vX[l_lIndexI - 1]) - l_dSig * l_vU[l_lIndexI - 1]) / l_dP;
            }

            if (p_dYpn == 1e30)
                l_dQn = l_dUn = 0.0;
            else
            {
                l_dQn = 0.5;
                l_dUn = (3.0 / (p_vX[p_lN - 1] - p_vX[p_lN - 2])) * (p_dYpn - (p_vY[p_lN - 1] - p_vY[p_lN - 2]) / (p_vX[p_lN - 1] - p_vX[p_lN - 2]));
            }

            p_vY2[p_lN - 1] = (l_dUn - l_dQn * l_vU[p_lN - 2]) / (l_dQn * p_vY2[p_lN - 2] + 1.0);
            for (l_lIndexK = p_lN - 2; l_lIndexK >= 0; l_lIndexK--)
                p_vY2[l_lIndexK] = p_vY2[l_lIndexK] * p_vY2[l_lIndexK + 1] + l_vU[l_lIndexK];
        }
        //fonction qui utilise les dérivées secondes calculées dans spline (->y2a) et renvoie l'interpolation y(x) 
        static public double splint(double[] p_vXa, double[] p_vYa, double[] p_vY2a, int p_lN, double p_dX)
        {
            int l_lKlo, l_lKhi, l_lK;
            double l_dH, l_dB, l_dA, l_dY;

            l_lKlo = 0;
            l_lKhi = p_lN - 1;

            while (l_lKhi - l_lKlo > 1)
            {
                l_lK = (l_lKhi + l_lKlo) >> 1;
                if (p_vXa[l_lK] > p_dX) l_lKhi = l_lK;
                else l_lKlo = l_lK;
            }

            l_dH = p_vXa[l_lKhi] - p_vXa[l_lKlo];
            if (l_dH == 0)
                return 0;

            l_dA = (p_vXa[l_lKhi] - p_dX) / l_dH;
            l_dB = (p_dX - p_vXa[l_lKlo]) / l_dH;

            l_dY = l_dA * p_vYa[l_lKlo] + l_dB * p_vYa[l_lKhi] + ((l_dA * l_dA * l_dA - l_dA) * p_vY2a[l_lKlo] + (l_dB * l_dB * l_dB - l_dB) * p_vY2a[l_lKhi]) * (l_dH * l_dH) / 6.0;

            return l_dY;
        }

        //fonction qui utilise les dérivées secondes calculées dans spline (->y2a) et renvoie l'interpolation y'(x) 
        static public double splintDerivative(double[] p_vXa, double[] p_vYa, double[] p_vY2a, int p_lN, double p_dX)
        {
            int l_lKlo, l_lKhi, l_lK;
            double l_dH, l_dB, l_dA, l_dY;

            l_lKlo = 0;
            l_lKhi = p_lN - 1;

            while (l_lKhi - l_lKlo > 1)
            {
                l_lK = (l_lKhi + l_lKlo) >> 1;
                if (p_vXa[l_lK] > p_dX) l_lKhi = l_lK;
                else l_lKlo = l_lK;
            }

            l_dH = p_vXa[l_lKhi] - p_vXa[l_lKlo];
            if (l_dH == 0)
                return 0;

            l_dA = (p_vXa[l_lKhi] - p_dX) / l_dH;
            l_dB = (p_dX - p_vXa[l_lKlo]) / l_dH;

            l_dY = -p_vYa[l_lKlo] / l_dH + p_vYa[l_lKhi] / l_dH + ((-3 * l_dA * l_dA + 1.0) * p_vY2a[l_lKlo] + (3 * l_dB * l_dB - 1.0) * p_vY2a[l_lKhi]) * l_dH / 6.0;

            return l_dY;
        }

        public void getValueAndDerivatives(double[] p_vX, ref double[] p_vY, ref double[] p_vY1, ref double[] p_vY2)
        {
            for (int l_lIndex = 0; l_lIndex < p_vX.Length; l_lIndex++)
                getValueAndDerivatives(p_vX[l_lIndex], ref p_vY[l_lIndex], ref p_vY1[l_lIndex], ref p_vY2[l_lIndex]);
        }

        //Renvoie les valeurs de y et ses 2 premières dérivées pour une spline naturelle
        public void getValueAndDerivatives(double p_dX, ref double p_dY, ref double p_dY1, ref double p_dY2)
        {
            if (m_eType != InterpolationType.E_Spline && m_eType != InterpolationType.E_SplineC)
                throw new Exception("Non implémenté pour les types != de E_Spline ou E_SplineC");

            if (m_eType == InterpolationType.E_SplineC)
            {
                if (p_dX > m_vX[m_vX.Length - 1] || p_dX < m_vX[0])
                {
                    p_dY = getY(p_dX);
                    p_dY1 = 0.0;
                    p_dY2 = 0.0;
                    return;
                }
            }

            if (m_bModif == true)
                buildDerivatives();

            int l_lKlo, l_lKhi, l_lK;
            double l_dH, l_dB, l_dA;

            l_lKlo = 0;
            l_lKhi = m_lN - 1;

            while (l_lKhi - l_lKlo > 1)
            {
                l_lK = (l_lKhi + l_lKlo) >> 1;
                if (m_vX[l_lK] > p_dX) l_lKhi = l_lK;
                else l_lKlo = l_lK;
            }

            l_dH = m_vX[l_lKhi] - m_vX[l_lKlo];
            if (l_dH == 0)
                throw new Exception("Bad dimensions");

            l_dA = (m_vX[l_lKhi] - p_dX) / l_dH;
            l_dB = (p_dX - m_vX[l_lKlo]) / l_dH;

            p_dY = l_dA * m_vY[l_lKlo] + l_dB * m_vY[l_lKhi] + ((l_dA * l_dA * l_dA - l_dA) * m_vSplineDer2[l_lKlo] + (l_dB * l_dB * l_dB - l_dB) * m_vSplineDer2[l_lKhi]) * (l_dH * l_dH) / 6.0;

            if (p_dX <= m_vX[0])
            {
                p_dY1 = m_vSplineDer[0];
                p_dY2 = 0.0;
            }
            else if (p_dX >= m_vX[m_vX.Length - 1])
            {
                p_dY1 = m_vSplineDer[m_vX.Length - 1];
                p_dY2 = 0.0;
            }
            else
            {
                p_dY1 = -m_vY[l_lKlo] / l_dH + m_vY[l_lKhi] / l_dH + ((-3 * l_dA * l_dA + 1.0) * m_vSplineDer2[l_lKlo] + (3 * l_dB * l_dB - 1.0) * m_vSplineDer2[l_lKhi]) * l_dH / 6.0;
                p_dY2 = l_dA * m_vSplineDer2[l_lKlo] + l_dB * m_vSplineDer2[l_lKhi];
            }
        }


    }
}


namespace BergomiTwoFactors
{
    public  class ParabolicVolatlityModel
        {
        double m_SigmaVariance, m_SigmaSkew, m_SigmaCurvature, m_SigmaAlpha, m_SigmaBeta, m_tenorPivot, m_volMinimum, m_MoneynessTurningPoint, m_lambda, m_T;

            public ParabolicVolatlityModel(double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha)
            {
                m_SigmaCurvature = SigmaCurvature;
                m_SigmaSkew = SigmaSkew;
                m_SigmaVariance = SigmaVariance;
                m_SigmaAlpha = SigmaAlpha;
                m_tenorPivot = 0.25;
                m_volMinimum = 0.05;
                m_SigmaBeta = 0.0;
                m_MoneynessTurningPoint = 0.0;
                m_lambda = 0.0;
                m_T = 0.0;
               
            }
            public ParabolicVolatlityModel(double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta)
            {
                m_SigmaCurvature = SigmaCurvature;
                m_SigmaSkew = SigmaSkew;
                m_SigmaVariance = SigmaVariance;
                m_SigmaAlpha = SigmaAlpha;
                m_SigmaBeta = SigmaBeta;
                m_tenorPivot = 0.25;
                m_volMinimum = 0.05;
                m_SigmaBeta = SigmaBeta;
                m_MoneynessTurningPoint = 0.0;
                m_lambda = 0.0;
                m_T = 0.0;
               
            }

            public double getVol(double moneyness, double T)  // moneyness = F0/K
            {
               
                double inflexion , result, SigmaVarianceEffective;

                SigmaVarianceEffective = m_SigmaBeta+m_SigmaVariance * Math.Pow(T / m_tenorPivot, m_SigmaAlpha);

                if (m_SigmaCurvature >= 0) 
                {
                    inflexion = 10.0;
                }
                else
                {
                    inflexion = -m_SigmaSkew /(2.0* m_SigmaCurvature);
                }
                if ( moneyness-1.0 >= inflexion)
                {
                    result = SigmaVarianceEffective - m_SigmaSkew*m_SigmaSkew/(4.0*m_SigmaCurvature);
                }
                else
                {
                    result = SigmaVarianceEffective + (moneyness - 1.0) * m_SigmaSkew + (moneyness - 1.0) * (moneyness - 1.0) * m_SigmaCurvature;
                }
                if(result < m_volMinimum)
                {
                    result = m_volMinimum;
                }

                return result;
            }
            public double ComputeTurningMoneyness(double T)
            {
                if(m_SigmaCurvature >= 0)  return 0.0;              
                double  SigmaVarianceEffective = m_SigmaBeta+m_SigmaVariance * Math.Pow(T / m_tenorPivot, m_SigmaAlpha);
                double TurningMoneyness =(2.0 * m_SigmaCurvature - m_SigmaSkew+Math.Sqrt(-4.0*SigmaVarianceEffective*m_SigmaCurvature+m_SigmaSkew*m_SigmaSkew+4.0*m_SigmaCurvature*m_volMinimum))/(2.0*m_SigmaCurvature);
                return TurningMoneyness;
            }

            public ParabolicVolatlityModel(double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta, double T)
            {
                m_T = T;
                m_SigmaCurvature = SigmaCurvature;
                m_SigmaSkew = SigmaSkew;
                m_SigmaVariance = SigmaVariance;
                m_SigmaAlpha = SigmaAlpha;
                m_SigmaBeta = SigmaBeta;
                m_tenorPivot = 0.25;
                m_volMinimum = 0.05;
                m_SigmaBeta = SigmaBeta;
                m_MoneynessTurningPoint = ComputeTurningMoneyness(T);
                m_lambda = (2.0 * (-1.0 + m_MoneynessTurningPoint) * m_SigmaCurvature + m_SigmaSkew) / m_volMinimum;
               

            }

            public double getVol2(double moneyness,double T)  // moneyness = F0/K
            {
                double result;
                if (moneyness <= m_MoneynessTurningPoint)
                    result = m_volMinimum * Math.Exp(m_lambda * (moneyness - m_MoneynessTurningPoint));
                else
                    result = getVol(moneyness, T);
               
                return result;
            }
        }

    

     public  class BergomiVolatilityModel
     {
         double mk1,  mk2,  mtheta,  mrho,  momega;
         double [] mforwardList;
         double mT,mT1,mdt;  
         Bergomi2factors.volatilityFunctor mImpliedVolFunc;

        public BergomiVolatilityModel(double k1, double k2, double theta,double  rho, double omega,double T,double T1,double dt,double [] forwardList, Bergomi2factors.volatilityFunctor ImpliedVolFunc)
        {
            mk1=k1;mk2=k2;mtheta=theta;mrho=rho;momega=omega;mT=T;mT1=T1;mdt=dt; mforwardList=forwardList;mImpliedVolFunc=ImpliedVolFunc;
        }
        public double getVol(double k ,double T)
        {
                return Bergomi2factors.BasketVolatility( mk1,  mk2,  mtheta,  mrho,  momega,mforwardList, mImpliedVolFunc,  T,  mT1,  mdt,  k);
        }

     }
     public class BergomiVolatilityModel2
     {
         double mk1, mk2, mtheta, mrho, momega, mSigmaVariance, mSigmaSkew, mSigmaCurvature, mSigmaAlpha, mSigmaBeta; int mNbLegendre, minterpolMod;
         double[] mvarList; double[] mdatelist;
         double mT, mT1, mT2, mdt;

         public BergomiVolatilityModel2(double k1, double k2, double theta, double rho, double omega, double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta, double T, double T1, double T2, double dt, double[] varList, double[] datelist, int NbLegendre, int interpolMod )
         {
             mk1 = k1; mk2 = k2; mtheta = theta; mrho = rho; momega = omega; mT = T; mT1 = T1; mT2 = T2; mdt = dt;
             mvarList = varList; mdatelist = datelist; mNbLegendre = NbLegendre; minterpolMod = interpolMod;
             mSigmaVariance = SigmaVariance; mSigmaSkew = SigmaSkew; mSigmaCurvature = SigmaCurvature; mSigmaAlpha = SigmaAlpha; mSigmaBeta = SigmaBeta;
         }
         public double getVol(double moneyness, double T)
         {
             return Bergomi2factors.Bergomi_VarianceSwapVolatility2(mk1, mk2, mtheta, mrho, momega,
                     mvarList, mdatelist, T, moneyness, mT1, mT2, mSigmaVariance, mSigmaSkew, mSigmaCurvature, mSigmaAlpha, mSigmaBeta, mdt, mNbLegendre, minterpolMod
                                );
         }

         public double getVol2(double moneyness, double T)
         {
             return Bergomi2factors.Bergomi_VarianceSwapVolatility22(mk1, mk2, mtheta, mrho, momega,
                     mvarList, mdatelist, T, moneyness, mT1, mT2, mSigmaVariance, mSigmaSkew, mSigmaCurvature, mSigmaAlpha, mSigmaBeta, mdt, mNbLegendre, minterpolMod
                                );
         }

     }

    public static class BergomiTest
    {
             // Declaration du foncteur de modele de volatilité
        public delegate double volatilityFunctor(double k,double T);
        

        [ExcelFunction(Description = "BlackSholes Call pricing ")]
        public static double Export_Call(double f, double k, double t, double v)
        {
            double option = BlackScholes.Call(f, k, t, v);
            return option;
        }

        [ExcelFunction(Description = "BlackSholes Put pricing ")]
        public static double Export_Put(double f, double k, double t, double v)
        {
            double option = BlackScholes.Put(f, k, t, v);
            return option;
        }

        [ExcelFunction(Description = "implicit Volatility for a vanilla pricing : type>=0 for a Call ")]
        public static double Export_BSImplicitVol(double f, double k, double t, double price, int type)
        {
            double vol = BlackScholes.ImpliedVolatility(f,k,t,price, type);
            return vol;
        }


        [ExcelFunction(Description = "Instantaneous  Covariance seen at maturity t between Ta and Tb ")]
        public static double Export_InstantaneousCovariance(
                    double omega,double k1,double k2,double theta,double rho,double t,double Ta,double Tb)
        {
            return Bergomi2factors.InstantaneousCovariance(omega, k1, k2, theta, rho, t, Ta, Tb);
        }

        [ExcelFunction(Description = "Instantaneous  Correlation seen at maturity t between Ta and Tb ")]
        public static double Export_ZetaCorrelation(double omega, double k1,double k2,double theta,double rho,double t,double Ta,double Tb)
        {
            return Bergomi2factors.ZetaCorrelation(omega, k1, k2, theta, rho, t, Ta, Tb);
        }

        [ExcelFunction(Description = "Vecteur de variance forward brute ")]
         public static double [] Export_VarianceSwap_RawForwardVariance(double[] varlist, double[] datelist)
        {
            return Bergomi2factors.VarianceSwap_RawForwardVariance(varlist, datelist);
        }


        [ExcelFunction(Description = "Variance forward brute ")]
        public static double Export_VarianceSwap_ForwardVariance(double[] varlist, double[] datelist, double T1, double T2, int interpolMod)
        {
            double[] varlistdenormalised = new double[varlist.Length];
            for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;

            double var= Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1, T2, interpolMod);
            return Math.Sqrt(var) * 100;
        }


        [ExcelFunction(Description = "Variance Swap vol  from a short term variance swap  smile  ")]
        public static double Export_ParabolicVol_VarianceSwap(double k1, double k2, double theta, double rho, double omega,
                               double[] varlist, double[] datelist,
                               double T, double T1, double T2,
                                int interpolmethod)
        {
            double[] varlistdenormalised = new double[varlist.Length];
            for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;           
            double forward = Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1, T2, interpolmethod);
            return 100 * forward;
        }

         [ExcelFunction(Description = "Variance Swap vol option  from a short term variance swap  smile  ")]
         public static double Export_ParabolicVol_VarianceSwapOption(double k1, double k2, double theta, double rho, double omega,
                                double[] varlist, double[] datelist,
                                double T, double K, double T1, double T2,
                                double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta, double dt, int NbLegendre, int VolOrOption,int optionType,
                                 int interpolmethod )
             // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
         {
             double[] varlistdenormalised = new double[varlist.Length];
             for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;

             double Kdenormalised = K / 100.0;
             double vol= Bergomi2factors.Bergomi_VarianceSwapVolatility( k1,  k2,  theta,  rho,  omega, varlistdenormalised, datelist, T, Kdenormalised,
                 T1, T2, SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha,SigmaBeta, dt, NbLegendre, interpolmethod);
             double result;
             if(VolOrOption==0)
             {
                result=100.0*vol;
             }
             else
             {
                 double forward = Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1, T2, interpolmethod);
             
                 if(optionType == -1)
                 {
                     result = 100 * BlackScholes.Put(forward, Kdenormalised, T, vol);
                 }
                 else
                 {
                     if(optionType == 1)
                     {
                         result = 100 * BlackScholes.Call(forward, Kdenormalised, T, vol);
                     }
                     else
                     {

                         if (Kdenormalised >= forward)
                             result = 100 * BlackScholes.Call(forward, Kdenormalised, T, vol);
                         else
                             result = 100 * BlackScholes.Put(forward, Kdenormalised, T, vol);

                     }
                 }
             }                       
             return result;
         }

       
        [ExcelFunction(Description = "Volatility Swap Volatility from a variance swap volatility smile  ")]
        public static double Export_ParabolicVol_VolSwap(
                               double T1, double T2, 
                               double k1, double k2, double theta, double rho, double omega,
                               double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta,double[] varlist, double[] datelist,
                               double integrationBoundUpFactor, double dt,
                               int n,int interpolMethod)
         // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
        
        {
            double[] varlistdenormalised = new double[varlist.Length];
            for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;

            double forward = Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1, T2, interpolMethod);
            
            int Ntranches = (int)Math.Floor((T2 - T1) / dt) + 1;
            double Modified_dt = (T2 - T1) / Ntranches;
            double[] forwardList = new double[Ntranches];
            for (int i = 0; i < Ntranches; i++)
            {
                forwardList[i] = Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1 + i * Modified_dt, T1 + (i + 1) * Modified_dt, interpolMethod);
            }

            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta);
            Bergomi2factors.volatilityFunctor ImpliedVolFunc = new Bergomi2factors.volatilityFunctor(model.getVol);
            double vol = Bergomi2factors.Bergomi_VarianceToVol(forward, k1, k2, theta, rho, omega, forwardList, ImpliedVolFunc, T1, dt, integrationBoundUpFactor, n);
            return 100*vol;
        }


        [ExcelFunction(Description = "Simple Square root option  ")]
        public static double Export_ParabolicVol_SimpleSquareRootCall(double forward,
                            double T, double K,
                            double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta,
                            double integrationBoundUpFactor,
                            int n, int VolOrOption)
        {
            double forwardDenormalise = forward / 100.0;
            double Kdenormalised = K / 100.0;
            double option = Bergomi2factors.ParabolicVol_SquareRootCall(forwardDenormalise,
                             T, Kdenormalised,
                             SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta,
                             integrationBoundUpFactor,
                             n);
            if (VolOrOption == 0)
            {
                double vol = BlackScholes.ImpliedVolatility(forwardDenormalise, Kdenormalised, T, option, 1);
                return 100 * vol;
            }
            else
            {
                return 100 * option;
            }
        }

        [ExcelFunction(Description = "Simple Square root option  ")]
        public static double Export_ParabolicVol_SimpleSquareRootCall2(double forward,
                            double T, double K,
                            double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta,
                            double integrationBoundUpFactor,
                            int n, int VolOrOption)
        {
            double forwardDenormalise = forward / 100.0;
            double Kdenormalised = K / 100.0;
            double option = Bergomi2factors.ParabolicVol_SquareRootCall2(forwardDenormalise,
                             T, Kdenormalised,
                             SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta,
                             integrationBoundUpFactor,
                             n);
            if (VolOrOption == 0)
            {
                double vol = BlackScholes.ImpliedVolatility(forwardDenormalise, Kdenormalised, T, option, 1);
                return 100 * vol;
            }
            else
            {
                return 100 * option;
            }
        }

        [ExcelFunction(Description = "Simple Square root option for the Bergomi  Variance Model  ")]
        public static double Export_Bergomi_SimpleSquareRootCall(double forward,
                            double T, double K,
                             double k1, double k2, double theta, double rho, double omega,
                              double T1, double T2, double [] varList, double [] datelist,
                            double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta, double dt,
                            double integrationBoundUpFactor,
                            int NbLegendre, int interpolMod,int VolOrOption)
        // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
        
        {
            double forwardDenormalise = forward / 100.0;
            double Kdenormalised = K / 100.0;
            double option = Bergomi2factors.Bergomi_SquareRootCall(forwardDenormalise, T, Kdenormalised,
                              k1, k2, theta, rho, omega,
                              T1, T2, dt, varList, datelist, 
                              SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha,SigmaBeta,
                              integrationBoundUpFactor, NbLegendre, interpolMod);

            if (VolOrOption == 0)
            {
                double vol = BlackScholes.ImpliedVolatility(forwardDenormalise, Kdenormalised, T, option, 1);
                return 100*vol;
            }
            else
            {
                return 100 * option;
            }
        }



        [ExcelFunction(Description = "Volatility Swap Volatility option avec un modele parabolique  ")]
        public static double Export_ParabolicVol_VolSwapOption(
                               double T, double T1, double T2, double K,
                               double k1, double k2, double theta, double rho, double omega,
                               double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta, double[] varlist, double[] datelist,
                               double integrationBoundUpFactor, double dt,
                               int n, int VolOrOption, int optiontype, int interpolMethod)
        // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
        {
            int NbLegendre = 15;
            double[] varlistdenormalised = new double[varlist.Length];
            for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;
            double Kdenormalised = K / 100.0;
            double Varforward = Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1, T2, interpolMethod);
            double forwardDenormalise = Math.Sqrt(Varforward);
            double option = Bergomi2factors.Bergomi_SquareRootCall(forwardDenormalise, T, Kdenormalised,
                              k1, k2, theta, rho, omega,
                              T1, T2, dt, varlist, datelist,
                              SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta,
                              integrationBoundUpFactor, NbLegendre, interpolMethod);

            if (VolOrOption == 0)
            {
                double vol = BlackScholes.ImpliedVolatility(forwardDenormalise, Kdenormalised, T, option, 1);
                return 100 * vol;
            }
            else
            {
                return 100 * option;
            }


        }

        [ExcelFunction(Description = "Volatility Swap Volatility option avec un modele parabolique  ")]
        public static double Export_ParabolicVol_VolSwapOption2(
                               double T, double T1, double T2, double K,
                               double k1, double k2, double theta, double rho, double omega,
                               double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta, double[] varlist, double[] datelist,
                               double integrationBoundUpFactor, double dt,
                               int n, int VolOrOption, int optiontype, int interpolMethod)
        // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
        {
            int NbLegendre = 15;
            double[] varlistdenormalised = new double[varlist.Length];
            for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;
            double Kdenormalised = K / 100.0;
            double Varforward = Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1, T2, interpolMethod);
            double forwardDenormalise = Math.Sqrt(Varforward);
            double option = Bergomi2factors.Bergomi_SquareRootCall2(forwardDenormalise, T, Kdenormalised,
                              k1, k2, theta, rho, omega,
                              T1, T2, dt, varlist, datelist,
                              SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta,
                              integrationBoundUpFactor, NbLegendre, interpolMethod);

            if (VolOrOption == 0)
            {
                double vol = BlackScholes.ImpliedVolatility(forwardDenormalise, Kdenormalised, T, option, 1);
                return 100 * vol;
            }
            else
            {
                return 100 * option;
            }


        }

        


        [ExcelFunction(Description = "Volatility Swap Volatility from a variance swap volatility smile  ")]
        public static double Export_BergomiVolSwapVolatility(
                               double T,double T1,double T2, double K,
                               double k1, double k2, double theta, double rho, double omega,
                               double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta, double[] varlist, double[] datelist,
                               double integrationBoundUpFactor,double dt,
                               int n, int interpolMethod)
        // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
        
        {
            double[] varlistdenormalised = new double[varlist.Length];
            for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;
            double Varstrike = (K / 100.0) * (K / 100.0);
            double Varforward = Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1, T2, interpolMethod);
            double VarMoneyness = Varstrike / Varforward;
            double vol=Bergomi2factors.Bergomi_VolSwapVolatility(
                              T,  T1,  T2,  VarMoneyness,
                              k1,  k2,  theta,  rho,  omega,
                              SigmaVol,  SigmaSkew,  SigmaCurvature,  SigmaAlpha, SigmaBeta, varlistdenormalised,  datelist,
                              integrationBoundUpFactor,  dt,
                              n,  interpolMethod);
             return 100*vol;
        }

        [ExcelFunction(Description = "Variance Swap Volatility input  ")]
        public static double Export_InputVarSwapVolatility(
                               double T, double moneyness, double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta,
                               double dt )
        // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
        
        {
            double effectiveVarMoneyness = moneyness * moneyness;
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta);
            Bergomi2factors.volatilityFunctor ImpliedVolFunc = new Bergomi2factors.volatilityFunctor(model.getVol);
            double vol = ImpliedVolFunc(effectiveVarMoneyness, T); 
            return 100 * vol;
        }

        [ExcelFunction(Description = "Variance Swap Volatility input  ")]
        public static double Export_InputVarSwapVolatility2(
                               double T, double moneyness, double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta,
                               double dt)
        // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
        {
            double effectiveVarMoneyness = moneyness * moneyness;
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta,T);
            Bergomi2factors.volatilityFunctor ImpliedVolFunc = new Bergomi2factors.volatilityFunctor(model.getVol2);
            double vol = ImpliedVolFunc(effectiveVarMoneyness,T);
            return 100 * vol;
        }

        [ExcelFunction(Description = "Futur de VIX Like ")]
        public static double Export_VarianceSwap_VIXLike(double[] varlist, double[] datelist, double T1, double tenor, double indexSpot, double indexforward, double spread, int interpolMethod)
        {
            double[] varlistdenormalised = new double[varlist.Length];
            for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;

            double x = (indexforward / indexSpot - 1.0);

            double sigma2 = Bergomi2factors.VarianceSwap_ForwardVariance(varlistdenormalised, datelist, T1, T1 + tenor, interpolMethod) - 1 / T1 * x * x;
            return 100.0 * Math.Sqrt(sigma2) + spread/100.0;
        }


        [ExcelFunction(Description = "Option sur future de VIX like  ")]
        public static double Export_ParabolicVol_VIXLikeOption(double[] varlist, double[] datelist,double VixForward,
                               double T, double K,
                               double k1, double k2, double theta, double rho, double omega,
                               double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta,
                               double integrationBoundUpFactor, double dt,
                               int n, int VolOrOption,
                               double tenor,int optiontype ,  int interpolMethod)
        // dt definit le court terme pour la construction du panier de forwardlet qui servira calculer la volatilité du swap de variance forward
        
        {
            double[] varlistdenormalised = new double[varlist.Length];
            for (int i = 0; i < varlist.Length; i++) varlistdenormalised[i] = varlist[i] * varlist[i] / 10000;

            double Kdenormalised = K / 100.0;
            double VixForwarddenormalised = VixForward / 100.0;
     
            double option = Bergomi2factors.Bergomi_SquareRootCall(VixForwarddenormalised, T, Kdenormalised,
                              k1, k2, theta, rho, omega,
                              T, T + tenor, dt, varlist, datelist, 
                              SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha,SigmaBeta,
                              integrationBoundUpFactor, n,interpolMethod);                
            double result=0.0;
            if (VolOrOption > 0)
            {               
                if (optiontype == 1) 
                {
                    result =  option;
                }
                else if (optiontype == -1)
                {
                    result = (option - (VixForwarddenormalised - Kdenormalised));
                }
                else if (optiontype == 0)
                {
                    if (VixForwarddenormalised < Kdenormalised)
                    {
                        result = (option - (VixForwarddenormalised - Kdenormalised));

                    }
                    else
                    {

                        result = option;
                    }
                }
               

            }
            else
            {
                result = BlackScholes.ImpliedVolatility(VixForwarddenormalised, Kdenormalised, T, option,1);
            }
            return 100 * result;
        }

       


        [ExcelFunction(Description = "Interpolation and extrapolation of forward curve ")]
         public static double Export_ForwardInterpolation(
              double[] forwardList,
              double[] DateList,         
              double T,
              int methode)
         {
             return Bergomi2factors.ForwardInterpolation(forwardList, DateList, T, methode);
         }

         [ExcelFunction(Description = "calcul d'un forward entre deux dates ")]
         public static double Export_ForwardIntegration(double[] forwardlist, double[] datelist, double T1,double T2, double pas_des_forwards,  int NbLegendre)
         {
             return Bergomi2factors.ForwardIntegration( forwardlist,  datelist,  T1, T2, pas_des_forwards,  NbLegendre);
         }
       

        
    }

    public class BlackScholes
    {
        public static double Call(double f, double k, double t, double v)
        {
            if (k == 0)
            {
                return f;
            }
            else
            {
                return f * CNumericalFunction.pdfNR((Math.Log(f / k) + v * v * t / 2.0) / (v * Math.Sqrt(t))) -
                            k * CNumericalFunction.pdfNR((Math.Log(f / k) - v * v * t / 2.0) / (v * Math.Sqrt(t)));
            }
        }

        public static double Call2(double f, double k, double t, double v)
        {
            if (k == 0)
            {
                return f;
            }
            else
            {
                return f * GaussianDistribution.gaussian_cumulative((Math.Log(f / k) + v * v * t / 2.0) / (v * Math.Sqrt(t))) -
                            k * GaussianDistribution.gaussian_cumulative((Math.Log(f / k) - v * v * t / 2.0) / (v * Math.Sqrt(t)));
            }
        }
        public static double Put(double f, double k, double t, double v)
        {
            return Call(f, k, t, v) - (f - k);
        }


        public static double ImpliedVolatility(double forward, double strike, double maturity, double raw_target_price, int type)
        {
            double IntrinseqVal;
            if (type == 0 || type == 1)
            {
                IntrinseqVal = Math.Max(forward - strike, 0.0);  // Call
            }
            else
            {
                if (type == -1)
                {
                    IntrinseqVal = Math.Max(strike - forward, 0.0); //Put
                }
                else
                {
                    if (strike >= forward)
                    {
                        IntrinseqVal = Math.Max(forward - strike, 0.0);  // Call
                    }
                    else
                    {
                        IntrinseqVal = Math.Max(strike - forward, 0.0); //Put
                    }
                }
            }
            double target_price = raw_target_price - IntrinseqVal;
            bool success;
            return ImpliedVolatility(forward, strike, maturity, target_price, 0.0, out success);
        }

        public static double ImpliedVolatility(double forward, double strike, double maturity, double target_price)
        {
            bool success;
            return ImpliedVolatility(forward, strike, maturity, target_price, 0.0, out success);
        }

        public static double ImpliedVolatility(double forward, double strike, double maturity, double target_price, out bool success)
        {
            return ImpliedVolatility(forward, strike, maturity, target_price, 0.0, out success);
        }

        public static double ImpliedVolatility(double forward, double strike, double maturity,
            double target_price, double volatility_guess, out bool success)
        {
            success = false;
            double relStrike = strike / forward; // relative strike
            double targetRelPrice = target_price / forward;  // relative price
            double intrinsicValue = Math.Max(1.0 - relStrike, 0);
            double targetTimeValue = targetRelPrice; //-intrinsicValue;

            if ((targetTimeValue <= 0.0) || targetTimeValue >= Math.Max(relStrike, 1))
                return -1.0;

            double sigma = (volatility_guess == 0.0) ? 0.3 : volatility_guess;
            double sqrtT = Math.Sqrt(maturity);
            double sigmaT = sigma * sqrtT;

            double dSigma = 0.1;
            double dSigmaMax = 1;
            int i = 0;

            while ((Math.Abs(dSigma) > 1e-5))
            {
                sigmaT = sigma * sqrtT;
                double d1 = -Math.Log(relStrike) / sigmaT + 0.5 * sigmaT;
                double d0 = d1 - sigmaT;
                double lfVega = Math.Max(sqrtT * GaussianDistribution.gaussian_density(d1), 1e-15);
                //! first order
                double timeValue = (relStrike > 1.0 ? GaussianDistribution.gaussian_cumulative(d1) - relStrike * GaussianDistribution.gaussian_cumulative(d0) :
                                                    relStrike * GaussianDistribution.gaussian_cumulative(-d0) - GaussianDistribution.gaussian_cumulative(-d1));
                dSigma = (targetTimeValue - timeValue) / Math.Max(lfVega, 1e-50);
                double lfVomaByVega = d1 * d0 / sigma;
                //! second order
                dSigma *= 2.0 / (1.0 + Math.Sqrt(Math.Max(1.0 + 2.0 * dSigma * lfVomaByVega, 0.0)));
                //dSigma *= 1.0 / (Math.Max(1.0 + 0.5 * dSigma * lfVomaByVega, 0.0));
                //! cap on dSigma, when vega(k, sigma) is around zero
                dSigma = Math.Min(dSigma, dSigmaMax);
                sigma += dSigma;
                dSigmaMax /= 2.0;
                i++;
            }

            success = (i == 0) ? false : true;

            return sigma;
        }

    }

    static class GaussianDistribution
    {
        private readonly static double[] epow;
        private readonly static double alpha, alpha2, sqrtplog10, dpi, invsqrt2pi;

        static GaussianDistribution()
        {
            int precision = 15; // pour le double !
            dpi = Math.PI;
            invsqrt2pi = 1.0 / Math.Sqrt(2.0 * dpi);
            double dlog10 = Math.Log(10.0);

            sqrtplog10 = Math.Sqrt(precision * dlog10);
            alpha = 0.5 * dpi / sqrtplog10;
            alpha2 = alpha * alpha;

            epow = new double[(int)Math.Ceiling(sqrtplog10 / alpha) + 1];

            double t2 = Math.Exp(-alpha2);
            double t3 = t2 * t2;
            double t4 = 1.0;

            // calculating the table of exp(-k^2*alpha^2)
            for (int i = 0; i < epow.Length; ++i)
            {
                t4 *= t2;
                epow[i] = t4;
                t2 *= t3;
            }
        }

        static public double chiarella_reichel_formula(double arg)
        {
            if (arg < 0.0)
            {
                return (arg == 0.0) ? 1.0 : 2.0 - chiarella_reichel_formula(-arg);
            }
            if (arg > 10000.0)
            {
                return 0.0;
            }

            if (dpi < alpha * arg)
            {// case of very big argument
                double d2 = dpi / arg;
                double d2_square = d2 * d2;
                int max_iteration = (int)Math.Ceiling(sqrtplog10 / d2) + 1;
                double tsquare = arg * arg;
                double exp_mtsquare = Math.Exp(-tsquare);
                double sum = 0.0;
                for (int k = 0; k < max_iteration; ++k)
                {
                    int k2 = k + 1;
                    double t5 = Math.Exp(-(double)((uint)k2 * (uint)k2 * d2_square)) / ((uint)k2 * (uint)k2 * d2_square + tsquare);
                    sum += t5;
                    if (Math.Abs(t5) < double.Epsilon * sum)
                    {
                        break;
                    }
                }
                double returned_value = exp_mtsquare * d2 * arg / dpi * (1 / tsquare + 2 * sum) + 2 / (1 - Math.Exp(2 * dpi * arg / d2));
                return returned_value;
            }
            else
            {// case of a smaller argument : table epow is used
                double tsquare = arg * arg;
                double exp_mtsquare = Math.Exp(-tsquare);
                double sum = 0.0;
                for (int k = 0; k < epow.Length; ++k)
                {
                    int k2 = k + 1;
                    double t5 = epow[k] / ((uint)k2 * (uint)k2 * alpha2 + tsquare);
                    sum += t5;
                    if (Math.Abs(t5) < double.Epsilon * sum)
                    {
                        break;
                    }
                }
                double returned_value = exp_mtsquare * alpha * arg / dpi * (1 / tsquare + 2 * sum) + 2 / (1 - Math.Exp(2 * dpi * arg / alpha));
                return returned_value;
            }
        }

        static public double error_function_0_expansion(double arg)
        {
            if (double.IsNaN(arg))
            {
                return arg;
            }

            int max_iteration = 100000;
            double t0 = 1.0;
            double t1 = 1.0;
            double arg_square = arg * arg;
            double i_fact = 1.0;
            int ds = 1;
            for (int i = 1; i < max_iteration; ++i)
            {
                ds = -ds;
                i_fact *= (double)i;
                t1 *= arg_square;
                double denom = (double)(ds * (2 * i + 1)) * i_fact;
                double increment = t1 / denom;
                t0 += increment;
                if (Math.Abs(increment) < double.Epsilon * t0 * 0.5)
                {
                    return 2 / Math.Sqrt(dpi) * arg * t0;
                }
            }

            //throw new Exception("To many iterations in error_function_0_expansion");

            return 0.0;
        }

        static public double gaussian_cumulative(double arg)
        {
            double arg_temp = arg / Math.Sqrt(2.0);
            if (arg_temp > 1.0)
            {
                double erfc_temp = 0.5 * chiarella_reichel_formula(arg_temp);
                return 1.0 - erfc_temp;
            }
            else if (arg_temp < -1.0)
            {
                double erfc_temp = 0.5 * chiarella_reichel_formula(-arg_temp);
                return erfc_temp;
            }
            else
            {
                double erf_temp = 0.5 * error_function_0_expansion(arg_temp);
                return 0.5 + erf_temp;
            }
        }

        static public double gaussian_density(double arg)
        {
            return invsqrt2pi * Math.Exp(-0.5 * arg * arg);
        }
    }
 
    public class Bergomi2factors
    {

        // Declaration du foncteur de modele de volatilité
        public delegate double volatilityFunctor(double k, double T);


        public static double InstantaneousCovariance(
                    double omega,
                    double k1,
                    double k2,
                    double theta,
                    double rho,
                    double t,
                    double Ta,
                    double Tb)
        {
            double tmina = t > Ta ? Ta : t;
            double tminb = t > Tb ? Tb : t;
            double tminaminb = tminb > Ta ? Ta : tminb;
            double h1 = Math.Exp(-k1 * (Ta + Tb - tmina - tminb)) * (1.0 - Math.Exp(-2.0 * k1 * tminaminb)) / k1;
            double h2 = theta * theta * Math.Exp(-k2 * (Ta + Tb - tmina - tminb)) * (1.0 - Math.Exp(-2.0 * k2 * tminaminb)) / k2;
            double h3 = 2.0 * theta * rho * (Math.Exp(-k1 * (Ta - tmina) - k2 * (Tb - tminb)) + Math.Exp(-k2 * (Ta - tmina) - k1 * (Tb - tminb))) * (1.0 - Math.Exp(-(k1 + k2) * tminaminb)) / (k1 + k2);

            return Math.Exp(omega * omega / 2.0 * (h1 + h2 + h3));
        }


        public static double ZetaCorrelation(
            double omega,
            double k1,
            double k2,
            double theta,
            double rho,
            double T,
            double Ti,
            double Tj)
        {
            double Eii = InstantaneousCovariance(omega, k1, k2, theta, rho, T, Ti, Ti);
            double Ejj = InstantaneousCovariance(omega, k1, k2, theta, rho, T, Tj, Tj);
            double Eij = InstantaneousCovariance(omega, k1, k2, theta, rho, T, Ti, Tj);
            return Eij / Math.Sqrt(Eii * Ejj);
        }

        public static double BasketVolatilityWithInterpolation(double k1, double k2, double theta, double rho, double omega, double[] forwardList, double[] dateList,
           volatilityFunctor ImpliedVolFunc, double T, double T1, double T2, double strike, int NbForwardlet, int NbLegendre)
        {
            double dt = (T2 - T1) / NbForwardlet;
            double [] dateList2 = new double[NbForwardlet];
            for(int i=0;i<NbForwardlet;i++) dateList2[i]=T1+dt*(T2-T1);
            double [] forwardList2=new double[NbForwardlet];
            for (int i = 0; i < NbForwardlet; i++) forwardList2[i] = ForwardIntegration(forwardList, dateList, T1 + i * dt, T1 + dt + i * dt, dt, NbLegendre);
            double forward=0.0;for(int i=0;i<NbForwardlet;i++) forward +=forwardList2[i];
            double relativebasketStrike=strike/forward-1.0;
            return BasketVolatility(k1, k2, theta, rho, omega, forwardList2,ImpliedVolFunc, T, T1, dt, relativebasketStrike);
        }

        public static double BasketVolatility(double k1, double k2, double theta, double rho, double omega, double[] forwardList,
            volatilityFunctor ImpliedVolFunc, double T, double T1, double dt, double relativebasketStrike)
        {
            int n = forwardList.Length;
            double [] VarianceLetDates; // 
            VarianceLetDates = new double[n];  // 
            for (int i = 0; i < n; i++) VarianceLetDates[i] = T1 + i * dt;
            double[,] corrMatrix;
            corrMatrix = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    corrMatrix[i,j]=ZetaCorrelation(omega,k1,k2,theta,rho,T,VarianceLetDates[i],VarianceLetDates[j]);
                }
            double B = 0.0;
            for (int i = 0; i < n; i++) B += forwardList[i]; B /= n;
            double[] spreadList = new double[n]; ;
            for (int i = 0; i < n; i++) spreadList[i] = (relativebasketStrike - 1.0);
            double[] RawsigmaiList; double[] sigmaiList;
            sigmaiList = new double[n]; RawsigmaiList = new double[n];
            for (int i = 0; i < n; i++) RawsigmaiList[i] = ImpliedVolFunc(1.0 + spreadList[i], T) ;
            for (int i = 0; i < n; i++) sigmaiList[i] = RawsigmaiList[i] * forwardList[i];
            double sigmaB = 0.0;
             for (int i = 0; i < n; i++)
                 for (int j = 0; j < n; j++)
                 {
                     sigmaB += corrMatrix[i, j] * sigmaiList[i] * sigmaiList[j];
                 }
             double Bn = B * n;
             sigmaB = Math.Sqrt(sigmaB) / Bn;
            
             double[] pi = new double[n]; 
             for (int i = 0; i < n; i++) pi[i] =forwardList[i]/ Bn;
             for (int i = 0; i < n; i++)
             {

                 spreadList[i] = sigmaiList[i] / (sigmaB * B) * (relativebasketStrike - 1.0);
                 double sum=0.0;
                 for (int j = 0; j < n; j++)
                     sum += corrMatrix[i, j] * pi[j] * sigmaiList[j] / (sigmaB* B);
                 spreadList[i] *= sum;
             }
             for (int i = 0; i < n; i++) RawsigmaiList[i] = ImpliedVolFunc(1.0 + spreadList[i], T);
             for (int i = 0; i < n; i++) sigmaiList[i] = RawsigmaiList[i] * forwardList[i];
             sigmaB = 0.0;
             for (int i = 0; i < n; i++)
                 for (int j = 0; j < n; j++)
                 {
                     sigmaB += corrMatrix[i, j] * sigmaiList[i] * sigmaiList[j];
                 }
             sigmaB = Math.Sqrt(sigmaB) / (B * n);
             return sigmaB;
        }

      
        public static double ParabolicVol_VarianceToVol(double Varforward,
                               double T,
                               double SigmaVariance, double SigmaSkew, double SigmaCurvature,double SigmaAlpha,double SigmaBeta,
                               double integrationBoundUpFactor,
                               int n)
        {
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol);
            return GenericVol_VarianceToVol(ImpliedVolFunc, Varforward, T, integrationBoundUpFactor, n);
        }

        public static double Bergomi_VarianceToVol(double forward,
                           double k1, double k2, double theta, double rho, double omega, double[] forwardList,
                           volatilityFunctor ImpliedVolFunc, double T1, double dt,
                           double integrationBoundUpFactor,
                           int n)
        {
            double T2 = T1 + dt * forwardList.Length;
            BergomiVolatilityModel model = new BergomiVolatilityModel(k1, k2, theta, rho, omega, T2, T1, dt, forwardList, ImpliedVolFunc);
            volatilityFunctor ImpliedVolFunc2 = new volatilityFunctor(model.getVol);
            return GenericVol_VarianceToVol(ImpliedVolFunc2, forward, T2, integrationBoundUpFactor, n);
        }

        public static double GenericVol_VarianceToVol(volatilityFunctor VarianceVarVolFunc, double forward,
                               double T,
                               double integrationBoundUpFactor,
                               int n)
        {
            CGaussLegendre gl = new CGaussLegendre(n);

            double[] Kput = new double[n];
            for (int i = 0; i < n; i++)
            {
                Kput[i] = gl.X[i] * forward;
            }

            double[] KputVols = new double[n];
            for (int i = 0; i < n; i++)
            {
                KputVols[i] = VarianceVarVolFunc(Kput[i] / forward,T);
            }          

            double[] Kcall = new double[n];
            for (int i = 0; i < n; i++)
            {
                Kcall[i] = forward + gl.X[i] * forward * (integrationBoundUpFactor - 1.0);
            }

            double[] KcallVols = new double[n];
            for (int i = 0; i < n; i++)
            {
                KcallVols[i] = VarianceVarVolFunc(Kcall[i] / forward,T);
            }   

            double result = Math.Sqrt(forward);
            double result1 = 0.0; double result2 = 0.0;

            
                for (int i = 0; i < n; i++)
                {
                    result1 -= forward * gl.W[i] * BlackScholes.Put(forward, Kput[i], T, KputVols[i]) / (4.0 * Math.Pow(Kput[i], 1.5));
                    result2 -= forward * (integrationBoundUpFactor - 1.0) * gl.W[i] * BlackScholes.Call(forward, Kcall[i], T, KcallVols[i]) / (4.0 * Math.Pow(Kcall[i], 1.5));
                }
            return result + result1 + result2;

        }

       
        public static double ParabolicVol_FromVarianceToVolCall(double forward,
                              double T,double K,
                              double SigmaVariance, double SigmaSkew, double SigmaCurvature,double SigmaAlpha,double SigmaBeta,
                              double integrationBoundUpFactor,
                              int n)
        {
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol);
            return GenericVol_SquareRootCall(ImpliedVolFunc, forward, T,K, integrationBoundUpFactor, n);
        }

        public static double BergomiVol_VolatilitySwapVol(double forward,
                             double T, double K,
                             double k1, double k2, double theta, double rho, double omega, double[] forwardList,
                             volatilityFunctor ImpliedVolFunc, double T1, double dt,
                             double integrationBoundUpFactor,
                             int n)
        {
            BergomiVolatilityModel model = new BergomiVolatilityModel(k1, k2, theta, rho, omega, T, T1, dt, forwardList, ImpliedVolFunc);
            volatilityFunctor ImpliedVolFunc2 = new volatilityFunctor(model.getVol);
            return GenericVol_FromVarianceToVol(ImpliedVolFunc2, forward, T, K, integrationBoundUpFactor, n);

        }

        public static double BergomiVol_VarSquareRootCall(double forward,
                             double T, double K,
                             double k1, double k2, double theta, double rho, double omega, double[] forwardList,
                             volatilityFunctor ImpliedVolFunc,double T1, double dt,
                             double integrationBoundUpFactor,
                             int n)
        {
            BergomiVolatilityModel model = new BergomiVolatilityModel(k1, k2, theta, rho, omega, T, T1, dt, forwardList, ImpliedVolFunc);
            volatilityFunctor ImpliedVolFunc2 = new volatilityFunctor(model.getVol);
            return GenericVol_SquareRootCall(ImpliedVolFunc2, forward, T, K, integrationBoundUpFactor, n);
        }

        public static double ParabolicVol_SquareRootCall(double forward,
                             double T, double K,
                             double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta,
                             double integrationBoundUpFactor,
                             int n)
        {
            double forward_sousentendu = forward * forward;
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol);
            return GenericVol_SquareRootCall(ImpliedVolFunc, forward_sousentendu, T, K, integrationBoundUpFactor, n);
        }

        public static double ParabolicVol_SquareRootCall2(double forward,
                             double T, double K,
                             double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta,
                             double integrationBoundUpFactor,
                             int n)
        {
            double forward_sousentendu = forward * forward;
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol2);
            return GenericVol_SquareRootCall(ImpliedVolFunc, forward_sousentendu, T, K, integrationBoundUpFactor, n);
        }

        public static double Bergomi_SquareRootCall(double forward,
                             double T, double K,
                             double k1, double k2, double theta, double rho, double omega,
                             double T1, double T2, double dt, double[] varList, double[] datelist,
                             double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta,
                             double integrationBoundUpFactor,
                             int NbLegendre, int interpolMod)
        {
            double forward_sousentendu = forward * forward;
            BergomiVolatilityModel2 model = new BergomiVolatilityModel2(k1, k2, theta, rho, omega, SigmaVariance, SigmaSkew, SigmaCurvature,
                SigmaAlpha, SigmaBeta, T, T1, T2, dt, varList, datelist, NbLegendre, interpolMod);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol);
            return GenericVol_SquareRootCall(ImpliedVolFunc, forward_sousentendu, T, K, integrationBoundUpFactor, NbLegendre);
        }

        public static double Bergomi_SquareRootCall2(double forward,
                             double T, double K,
                             double k1, double k2, double theta, double rho, double omega,
                             double T1, double T2, double dt, double[] varList, double[] datelist,
                             double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta,
                             double integrationBoundUpFactor,
                             int NbLegendre, int interpolMod)
        {
            double forward_sousentendu = forward * forward;
            BergomiVolatilityModel2 model = new BergomiVolatilityModel2(k1, k2, theta, rho, omega, SigmaVariance, SigmaSkew, SigmaCurvature,
                SigmaAlpha, SigmaBeta, T, T1, T2, dt, varList, datelist, NbLegendre, interpolMod);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol);
            return GenericVol_SquareRootCall(ImpliedVolFunc, forward_sousentendu, T, K, integrationBoundUpFactor, NbLegendre);
        }

        public static double GenericVol_FromVarianceToVol(volatilityFunctor ImpliedVolFunc, double forward,
                             double T, double K,
                             double integrationBoundUpFactor,
                             int n)
        {
            double option = GenericVol_SquareRootCall(ImpliedVolFunc, forward, T, K, integrationBoundUpFactor, n);
            double vol = BlackScholes.ImpliedVolatility(Math.Sqrt(forward), K, T, option, 1);
            return vol;

        }


        public static double GenericVol_SquareRootCall(volatilityFunctor ImpliedVolFunc, double forward,
                              double T,double K,
                              double integrationBoundUpFactor,
                              int n)
        {
            double Ksquared = K * K;
            CGaussLegendre gl = new CGaussLegendre(n);
            double result = Math.Max(0.0, Math.Sqrt(forward) - K);
            double result1 = 0.0; double result2 = 0.0; double result0;
            double[] Kput = new double[n]; double[] KputVols = new double[n]; double[] Kcall = new double[n]; double[] KcallVols = new double[n];
            double[] KcallOptions = new double[n]; double[] KputOptions = new double[n];
            double volAtKsquared = ImpliedVolFunc(Ksquared / forward,T);
            if (Ksquared < forward)
            {
               
                result0 = BlackScholes.Put(forward, Ksquared, T, volAtKsquared) / (2.0 * K);
             
                for (int i = 0; i < n; i++)
                {
                    Kput[i] = Ksquared + gl.X[i] * (forward - Ksquared);
                    KputVols[i] = ImpliedVolFunc(Kput[i] / forward,T);
                    KputOptions[i] = BlackScholes.Put(forward, Kput[i], T, KputVols[i]);
                }
                for (int i = 0; i < n; i++)
                {
                    Kcall[i] = forward + gl.X[i] * forward * (integrationBoundUpFactor - 1.0);
                    KcallVols[i] = ImpliedVolFunc(Kcall[i] / forward,T);
                    KcallOptions[i] = BlackScholes.Call(forward, Kcall[i], T, KcallVols[i]);
                }

                for (int i = 0; i < n; i++)
                {
                    result1 -= (forward - Ksquared) * gl.W[i] * BlackScholes.Put(forward, Kput[i], T, KputVols[i]) / (4.0 * Math.Pow(Kput[i], 1.5));
                    result2 -= forward * (integrationBoundUpFactor - 1.0) * gl.W[i] * BlackScholes.Call(forward, Kcall[i], T, KcallVols[i]) / (4.0 * Math.Pow(Kcall[i], 1.5));
                }
            }
            else
            {
           
                result0 = BlackScholes.Call(forward, Ksquared, T, volAtKsquared) / (2.0 * K);
               
                for (int i = 0; i < n; i++)
                {
                    Kcall[i] = Ksquared + gl.X[i] * (forward * integrationBoundUpFactor - Ksquared);
                    KcallVols[i] = ImpliedVolFunc(Kcall[i] / forward,T);
                    KcallOptions[i] = BlackScholes.Call(forward, Kcall[i], T, KcallVols[i]);
                }
                for (int i = 0; i < n; i++)
                {
                     result2 -= (forward * integrationBoundUpFactor - Ksquared) * gl.W[i] * BlackScholes.Call(forward, Kcall[i], T, KcallVols[i]) / (4.0 * Math.Pow(Kcall[i], 1.5));
                }
            }
            return result + result0 + result1 + result2;

        }   

        public static double ForwardInterpolation(double [] forwardlist, double [] datelist, double T, int method)
            {
                Interpolation.InterpolationType interType = Interpolation.InterpolationType.E_Spline;
                if (method == 0) interType = Interpolation.InterpolationType.E_Spline;
                if (method == 1) interType = Interpolation.InterpolationType.E_Linear;
                if (method == 2) interType = Interpolation.InterpolationType.E_LinearC;
                if (method == 3) interType = Interpolation.InterpolationType.E_SplineC;
                if (method == 4) interType = Interpolation.InterpolationType.E_SplineL;
                if (method == 5) interType = Interpolation.InterpolationType.E_SplineSqrLog;
                if (method == 6) interType = Interpolation.InterpolationType.E_SplineSqrLogC;
                if (method == 7) interType = Interpolation.InterpolationType.E_SplineVol;
                if (method == 8) interType = Interpolation.InterpolationType.E_Constant;
                              
                Interpolation.InterpolationStructure inter = new Interpolation.InterpolationStructure(datelist, forwardlist, interType);
                double f = inter.getY(T);
                return f;
          
            }

        public static double ForwardIntegration(double[] forwardlist, double[] datelist, double T1,double T2, double pas_des_forward, int NbLegendre)
        {
            CGaussLegendre gl = new CGaussLegendre(NbLegendre);
            double[] t = new double[NbLegendre];
            for (int i = 0; i < NbLegendre; i++)
            {
                t[i] = T1 + gl.X[i] * (T2 - T1);
            }          
            double[] f;           
            Interpolation.InterpolationType interType = Interpolation.InterpolationType.E_Spline;
            Interpolation.InterpolationStructure inter = new Interpolation.InterpolationStructure(datelist, forwardlist, interType);
            f = inter.getY(t);
            double result=0;
            for (int i = 0; i < NbLegendre; i++)
            {
                result += (T2 - T1) * gl.W[i] * f[i];
            }
            return result / pas_des_forward;
        }      

        public static double [] VarianceSwap_RawForwardVariance(double[] varlist, double[] datelist)
        {
            int Nbdata = varlist.Length;
            double [] rawvariance=new double[Nbdata];
            rawvariance[0] = varlist[0] * varlist[0] * datelist[0];
            for (int i = 1; i < Nbdata; i++)
            {
                rawvariance[i] = varlist[i] * varlist[i] * datelist[i] - varlist[i - 1] * varlist[i - 1] * datelist[i - 1];
            }
            return rawvariance;
        }

        public static double VarianceSwap_ForwardVariance(double[] varlist, double[] datelist, double T1, double T2, int interolMod)
        {
            int Nbdata = varlist.Length;
            double[] rawvariance = new double[Nbdata];
            for (int i = 0; i < Nbdata; i++)
            {
                rawvariance[i] = varlist[i] * datelist[i];
            }

            Interpolation.InterpolationType interType = Interpolation.InterpolationType.E_Spline;
            if (interolMod == 0) interType = Interpolation.InterpolationType.E_Spline;
            if (interolMod == 1) interType = Interpolation.InterpolationType.E_Linear;
            if (interolMod == 2) interType = Interpolation.InterpolationType.E_LinearC;
            if (interolMod == 3) interType = Interpolation.InterpolationType.E_SplineC;
            if (interolMod == 4) interType = Interpolation.InterpolationType.E_SplineL;
            if (interolMod == 5) interType = Interpolation.InterpolationType.E_SplineSqrLog;
            if (interolMod == 6) interType = Interpolation.InterpolationType.E_SplineSqrLogC;
            if (interolMod == 7) interType = Interpolation.InterpolationType.E_SplineVol;
            if (interolMod == 8) interType = Interpolation.InterpolationType.E_Constant;
                                     
            double[] Tboundary = new double[2]; double[] Vboundary = new double[2]; Tboundary[0] = T1; Tboundary[1] = T2;      
            Interpolation.InterpolationStructure inter = new Interpolation.InterpolationStructure(datelist, rawvariance, interType);
            Vboundary = inter.getY(Tboundary);
            // la convention etant de parler en annualisé , le framework etant lineaire en temps , on normalise:
            return (Vboundary[1] - Vboundary[0])/(T2-T1);

        }
        

        public static double Bergomi_VarianceSwapVolatility(double k1, double k2, double theta, double rho, double omega,
                                double[] varList, double[] datelist,
                                double T,double K, double T1, double T2,
                                double SigmaVariance, double SigmaSkew, double SigmaCurvature,double SigmaAlpha,double SigmaBeta, double dt,int NbLegendre,int interpolMod
                                )
        {
            // K est suppose normalisé (typiquement 0.22)
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol);
            double forward = VarianceSwap_ForwardVariance(varList, datelist, T1, T2, interpolMod);
            double relativebasketStrike = K / forward-1.0;
            int Ntranches = Math.Min(20,(int)Math.Floor((T2 - T1) / dt) + 1);
            double Modified_dt=(T2-T1)/Ntranches;
            double[] forwardList =  new double[Ntranches];
            for (int i = 0; i < Ntranches; i++)
            {
                forwardList[i] = VarianceSwap_ForwardVariance(varList, datelist, T1 + i * Modified_dt, T1 + (i + 1) * Modified_dt, interpolMod) * Modified_dt;
            }
            double vol= BasketVolatility(k1, k2, theta, rho, omega, forwardList,ImpliedVolFunc, T, T1, Modified_dt, relativebasketStrike);
            return vol ;
        }

        public static double Bergomi_VarianceSwapVolatility2(double k1, double k2, double theta, double rho, double omega,
                                double[] varList, double[] datelist,
                                double T, double moneyness, double T1, double T2,
                                double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta, double dt, int NbLegendre, int interpolMod
                                )
        {
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol);
            int Ntranches = Math.Min(20, (int)Math.Floor((T2 - T1) / dt) + 1);
            double Modified_dt = (T2 - T1) / Ntranches;
            double[] forwardList = new double[Ntranches];
            for (int i = 0; i < Ntranches; i++)
            {
                forwardList[i] = VarianceSwap_ForwardVariance(varList, datelist, T1 + i * Modified_dt, T1 + (i + 1) * Modified_dt, interpolMod) * Modified_dt;
            }
            double vol = BasketVolatility(k1, k2, theta, rho, omega, forwardList, ImpliedVolFunc, T, T1, Modified_dt, moneyness);
            return vol;
        }

        public static double Bergomi_VarianceSwapVolatility22(double k1, double k2, double theta, double rho, double omega,
                                double[] varList, double[] datelist,
                                double T, double moneyness, double T1, double T2,
                                double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha, double SigmaBeta, double dt, int NbLegendre, int interpolMod
                                )
        {
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha, SigmaBeta,T);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol2);
            int Ntranches = Math.Min(20, (int)Math.Floor((T2 - T1) / dt) + 1);
            double Modified_dt = (T2 - T1) / Ntranches;
            double[] forwardList = new double[Ntranches];
            for (int i = 0; i < Ntranches; i++)
            {
                forwardList[i] = VarianceSwap_ForwardVariance(varList, datelist, T1 + i * Modified_dt, T1 + (i + 1) * Modified_dt, interpolMod) * Modified_dt;
            }
            double vol = BasketVolatility(k1, k2, theta, rho, omega, forwardList, ImpliedVolFunc, T, T1, Modified_dt, moneyness);
            return vol;
        }

        public static double Bergomi_VarianceSwapOption(double k1, double k2, double theta, double rho, double omega,
                                double[] varList, double[] datelist,
                                double T, double K, double T1, double T2,
                                double SigmaVariance, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta, double dt, int NbLegendre, int VolOrOption, int optiontype, int interpolMod
                                )
        {
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVariance, SigmaSkew, SigmaCurvature, SigmaAlpha,SigmaBeta);
            volatilityFunctor ImpliedVolFunc = new volatilityFunctor(model.getVol);
            double forward = VarianceSwap_ForwardVariance(varList, datelist, T1, T2, interpolMod);
            double relativebasketStrike = K / forward;
            int Ntranches = Math.Min(20, (int)Math.Floor((T2 - T1) / dt) + 1);
            double Modified_dt = (T2 - T1) / Ntranches;
            double[] forwardList = new double[Ntranches];
            for (int i = 0; i < Ntranches; i++)
            {
                forwardList[i] = VarianceSwap_ForwardVariance(varList, datelist, T1 + i * Modified_dt, T1 + (i + 1) * Modified_dt, interpolMod) * Modified_dt;
            }
            double vol = BasketVolatility(k1, k2, theta, rho, omega, forwardList, ImpliedVolFunc, T, T1, Modified_dt, relativebasketStrike);
            double result;
            if (VolOrOption > 0)
            {
                
                if (optiontype == -1)
                {
                    result = BlackScholes.Put(forward, K, T, vol);
                }
                else
                {
                    if (optiontype == 1)
                    {
                        result = BlackScholes.Call(forward, K, T, vol);
                    }
                    else
                    {
                        if (K > forward)
                        {
                            result = BlackScholes.Call(forward, K, T, vol);
                        }
                        else
                        {
                            result = BlackScholes.Put(forward, K, T, vol);
                        }
                    }
                }
            }
            else
            {    
                result = vol;
            }
            return result;
        }      
         
        public static double Bergomi_VolSwapOption(
                               double T,double T1,double T2, double K,
                               double k1, double k2, double theta, double rho, double omega,
                               double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta, double[] volList, double[] datelist,
                               double integrationBoundUpFactor,double dt,
                               int n, int VolOrOption, int optiontype, int interpolMethod
                                )
                {
                    double Varforward = Bergomi2factors.VarianceSwap_ForwardVariance(volList, datelist, T1, T2, interpolMethod);
                    ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha,SigmaBeta);
                    Bergomi2factors.volatilityFunctor ImpliedVolFunc = new Bergomi2factors.volatilityFunctor(model.getVol);
     //               int Ntranches = (int)Math.Floor((T2 - T1) / dt) + 1;
                    int Ntranches = 15;
                    double Modified_dt = (T2 - T1) / Ntranches;
                    double[] forwardList = new double[Ntranches];
                    for (int i = 0; i < Ntranches; i++)
                    {
                        forwardList[i] = Bergomi2factors.VarianceSwap_ForwardVariance(volList, datelist, T1 + i * Modified_dt, T1 + (i + 1) * Modified_dt, interpolMethod) * Modified_dt;
                    }
                    double equivalentKvol = K * (T2 - T1);
                    double volforward = Math.Sqrt(Varforward) * (T2 - T1);

                    double relativebasketStrike = equivalentKvol / volforward - 1.0;
                    double option = Bergomi2factors.BergomiVol_VarSquareRootCall(Varforward, T, K,k1,k2,theta,rho,omega,forwardList, ImpliedVolFunc,T1,dt, integrationBoundUpFactor, n);
                    double result;
                    if (VolOrOption > 0)
                    {
                        if (optiontype == -1)
                        {
                            result =  (option - (volforward - equivalentKvol));
                        }
                        else
                        {
                            if (optiontype == 1)
                         {
                             result =  option;
                            }
                         else               
                            {
                                if (equivalentKvol > volforward)
                                {
                                  result =  option;
                                 }
                                else
                                 {
                                     result =  (option - (volforward - equivalentKvol));
                                }
                             }
                        }
                    }
                    else
                    {
                        double reducedK = 100*equivalentKvol / volforward;
                        double reducedOpt = 100*option / volforward;
                        result = BlackScholes.ImpliedVolatility(100, reducedK, T, reducedOpt, 1);
                    }
                    return result;
                }


        public static double Bergomi_VolSwapVolatility(
                             double T, double T1, double T2, double VarMoneyness,
                             double k1, double k2, double theta, double rho, double omega,
                             double SigmaVol, double SigmaSkew, double SigmaCurvature, double SigmaAlpha,double SigmaBeta, double[] volList, double[] datelist,
                             double integrationBoundUpFactor, double dt,
                             int n, int interpolMethod
                              )
        {
            ParabolicVolatlityModel model = new ParabolicVolatlityModel(SigmaVol, SigmaSkew, SigmaCurvature, SigmaAlpha,SigmaBeta);
            Bergomi2factors.volatilityFunctor ImpliedVolFunc = new Bergomi2factors.volatilityFunctor(model.getVol);
            int Ntranches =Math.Min(20, (int)Math.Floor((T2 - T1) / dt) + 1);           
            double Modified_dt = (T2 - T1) / Ntranches;
            double[] forwardList = new double[Ntranches];
            for (int i = 0; i < Ntranches; i++)
            {
                forwardList[i] = Bergomi2factors.VarianceSwap_ForwardVariance(volList, datelist, T1 + i * Modified_dt, T1 + (i + 1) * Modified_dt, interpolMethod) * Modified_dt;
            }                     
            BergomiVolatilityModel model2 = new BergomiVolatilityModel(k1, k2, theta, rho, omega, T, T1, dt, forwardList, ImpliedVolFunc);
            volatilityFunctor ImpliedVolFunc2 = new volatilityFunctor(model2.getVol);
            double result = ImpliedVolFunc2(VarMoneyness, T);           
            return result;
        }
        
    }

}