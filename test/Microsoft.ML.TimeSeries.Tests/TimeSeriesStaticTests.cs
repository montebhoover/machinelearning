﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.ML.Runtime.Api;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.Runtime.TimeSeriesProcessing;
using Microsoft.ML.StaticPipe;
using Xunit;

namespace Microsoft.ML.Tests
{
    public sealed class TimeSeriesStaticTests
    {

        private sealed class Prediction
        {
#pragma warning disable CS0649
            [VectorType(4)]
            public double[] Change;
#pragma warning restore CS0649
        }

        private sealed class Data
        {
            public float Value;

            public Data(float value) => Value = value;
        }

        [Fact]
        public void ChangeDetection()
        {
            var env = new MLContext(conc: 1);
            const int size = 10;
            List<Data> data = new List<Data>(size);
            var dataView = env.CreateStreamingDataView(data);
            for (int i = 0; i < size / 2; i++)
                data.Add(new Data(5));

            for (int i = 0; i < size / 2; i++)
                data.Add(new Data((float)(5 + i * 1.1)));

            // Transition to the statically-typed data view.
            var staticData = dataView.AssertStatic(env, c => Value: c.R4.Scalar);

            // Build the pipeline
            var staticLearningPipeline = staticData.MakeNewEstimator()
                .Append(r => r.Value.IidChangePointDetect(80, size);

            // Train
            var detector = staticLearningPipeline.Fit(staticData);

            // Transform
            var output = detector.Transform(dataView);

            // Get predictions
            var enumerator = output.AsEnumerable<Prediction>(env, true).GetEnumerator();
            Prediction row = null;
            List<double> expectedValues = new List<double>() { 0, 5, 0.5, 5.1200000000000114E-08, 0, 5, 0.4999999995, 5.1200000046080209E-08, 0, 5, 0.4999999995, 5.1200000092160303E-08,
                0, 5, 0.4999999995, 5.12000001382404E-08};
            int index = 0;
            while (enumerator.MoveNext() && index < expectedValues.Count)
            {
                row = enumerator.Current;

                Assert.Equal(expectedValues[index++], row.Change[0]);
                Assert.Equal(expectedValues[index++], row.Change[1]);
                Assert.Equal(expectedValues[index++], row.Change[2]);
                Assert.Equal(expectedValues[index++], row.Change[3]);
            }
        }
    }
}
