/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using Python.Runtime;
using NUnit.Framework;

namespace QuantConnect.Tests.Python
{
    [TestFixture, Category("TravisExclude")]
    public class PythonPackagesTests
    {
        [Test]
        public void Peft()
        {
            AssertCode(@"
def RunTest():
    from transformers import AutoModelForSeq2SeqLM
    from peft import get_peft_config, get_peft_model, LoraConfig, TaskType
    model_name_or_path = ""bigscience/mt0-large""
    tokenizer_name_or_path = ""bigscience/mt0-large""

    peft_config = LoraConfig(
        task_type=TaskType.SEQ_2_SEQ_LM, inference_mode=False, r=8, lora_alpha=32, lora_dropout=0.1
    )");
        }

        [Test]
        public void Accelerator()
        {
            AssertCode(@"
def RunTest():
	import torch
	import torch.nn.functional as F
	from datasets import load_dataset
	from accelerate import Accelerator

	accelerator = Accelerator()
	device = accelerator.device

	model = torch.nn.Transformer().to(device)
	optimizer = torch.optim.Adam(model.parameters())
");
        }

        [Test, Explicit("ASD")]
        public void alibi_detect()
        {
            AssertCode(@"
def RunTest():
	from alibi_detect.datasets import fetch_cifar10c

	corruption = ['gaussian_noise', 'motion_blur', 'brightness', 'pixelate']
	X, y = fetch_cifar10c(corruption=corruption, severity=5, return_X_y=True)");
        }

        [Test]
        public void PytorchTabnet()
        {
            AssertCode(@"
def RunTest():
    from pytorch_tabnet.tab_model import TabNetClassifier, TabNetRegressor

    clf = TabNetClassifier()");
        }

        [Test]
        public void FeatureEngine()
        {
            AssertCode(@"
def RunTest():
	import pandas as pd
	from feature_engine.encoding import RareLabelEncoder

	data = {'var_A': ['A'] * 10 + ['B'] * 10 + ['C'] * 2 + ['D'] * 1}
	data = pd.DataFrame(data)
	data['var_A'].value_counts()
	rare_encoder = RareLabelEncoder(tol=0.10, n_categories=3)
	data_encoded = rare_encoder.fit_transform(data)
	data_encoded['var_A'].value_counts()");
        }

        [Test]
        public void Nolds()
        {
            AssertCode(@"
def RunTest():
    import nolds
    import numpy as np

    rwalk = np.cumsum(np.random.random(1000))
    h = nolds.dfa(rwalk)");
        }

        [Test]
        public void Pgmpy()
        {
            AssertCode(@"
def RunTest():
    from pgmpy.base import DAG
    G = DAG()
    G.add_node(node='a')
    G.add_nodes_from(nodes=['a', 'b'])");
        }

        [Test]
        public void Control()
        {
            AssertCode(@"
def RunTest():
    import numpy as np
    import control

    num1 = np.array([2])
    den1 = np.array([1, 0])
    num2 = np.array([3])
    den2 = np.array([4, 1])
    H1 = control.tf(num1, den1)
    H2 = control.tf(num2, den2)

    H = control.series(H1, H2)");
        }

        [Test]
        public void PyCaret()
        {
            AssertCode(@"
from pycaret.datasets import get_data
from pycaret.classification import setup

def RunTest():
    data = get_data('diabetes')
    s = setup(data, target = 'Class variable', session_id = 123)");
        }

        [Test]
        public void NGBoost()
        {
            AssertCode(@"
def RunTest():
	from ngboost import NGBClassifier
	from ngboost.distns import k_categorical, Bernoulli
	from sklearn.datasets import load_breast_cancer
	from sklearn.model_selection import train_test_split

	X, y = load_breast_cancer(return_X_y=True)
	y[0:15] = 2 # artificially make this a 3-class problem instead of a 2-class problem
	X_cls_train, X_cls_test, Y_cls_train, Y_cls_test = train_test_split(X, y, test_size=0.2)

	ngb_cat = NGBClassifier(Dist=k_categorical(3), verbose=False) # tell ngboost that there are 3 possible outcomes
	_ = ngb_cat.fit(X_cls_train, Y_cls_train) # Y should have only 3 values: {0,1,2}");
        }

        [Test]
        public void MLFlow()
        {
            AssertCode(@"
def RunTest():
    import mlflow
    from mlflow.models import infer_signature

    import pandas as pd
    from sklearn import datasets
    from sklearn.model_selection import train_test_split
    from sklearn.linear_model import LogisticRegression
    from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score


    # Load the Iris dataset
    X, y = datasets.load_iris(return_X_y=True)

    # Split the data into training and test sets
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42
    )

    # Define the model hyperparameters
    params = {
        ""solver"": ""lbfgs"",
        ""max_iter"": 1000,
        ""multi_class"": ""auto"",
        ""random_state"": 8888,
    }

    # Train the model
    lr = LogisticRegression(**params)
    lr.fit(X_train, y_train)

    # Predict on the test set
    y_pred = lr.predict(X_test)

    # Calculate metrics
    accuracy = accuracy_score(y_test, y_pred)");
        }

        [Test]
        public void TPOT()
        {
            AssertCode(@"
def RunTest():
    from tpot import TPOTClassifier
    from sklearn.datasets import load_digits
    from sklearn.model_selection import train_test_split

    digits = load_digits()
    X_train, X_test, y_train, y_test = train_test_split(digits.data, digits.target,
                                                        train_size=0.75, test_size=0.25)

    pipeline_optimizer = TPOTClassifier(generations=5, population_size=2, cv=5,
                                        random_state=42, verbosity=2)
    pipeline_optimizer.fit(X_train, y_train)
    print(pipeline_optimizer.score(X_test, y_test))
    pipeline_optimizer.export('tpot_exported_pipeline.py')");
        }

        [Test, Explicit("Needs to be run by itself to avoid hanging")]
        public void XTransformers()
        {
            AssertCode(
                @"
import torch
from x_transformers import XTransformer

def RunTest():
    model = XTransformer(
        dim = 512,
        enc_num_tokens = 256,
        enc_depth = 6,
        enc_heads = 8,
        enc_max_seq_len = 1024,
        dec_num_tokens = 256,
        dec_depth = 6,
        dec_heads = 8,
        dec_max_seq_len = 1024,
        tie_token_emb = True      # tie embeddings of encoder and decoder
    )

    src = torch.randint(0, 256, (1, 1024))
    src_mask = torch.ones_like(src).bool()
    tgt = torch.randint(0, 256, (1, 1024))

    loss = model(src, tgt, mask = src_mask) # (1, 1024, 512)
    loss.backward()");
        }

        [Test]
        public void Functime()
        {
            AssertCode(
                @"
import polars as pl
from functime.cross_validation import train_test_split
from functime.seasonality import add_fourier_terms
from functime.forecasting import linear_model
from functime.preprocessing import scale
from functime.metrics import mase

def RunTest():
    # Load commodities price data
    y = pl.read_parquet(""https://github.com/functime-org/functime/raw/main/data/commodities.parquet"")
    entity_col, time_col = y.columns[:2]

    # Time series split
    y_train, y_test = y.pipe(train_test_split(test_size=3))

    # Fit-predict
    forecaster = linear_model(freq=""1mo"", lags=24)
    forecaster.fit(y=y_train)
    y_pred = forecaster.predict(fh=3)

    # functime ❤️ functional design
    # fit-predict in a single line
    y_pred = linear_model(freq=""1mo"", lags=24)(y=y_train, fh=3)

    # Score forecasts in parallel
    scores = mase(y_true=y_test, y_pred=y_pred, y_train=y_train)

    # Forecast with target transforms and feature transforms
    forecaster = linear_model(
        freq=""1mo"",
        lags=24,
        target_transform=scale(),
        feature_transform=add_fourier_terms(sp=12, K=6)
    )

    # Forecast with exogenous regressors!
    # Just pass them into X
    X = (
        y.select([entity_col, time_col])
        .pipe(add_fourier_terms(sp=12, K=6)).collect()
    )
    X_train, X_future = y.pipe(train_test_split(test_size=3))
    forecaster = linear_model(freq=""1mo"", lags=24)
    forecaster.fit(y=y_train, X=X_train)
    y_pred = forecaster.predict(fh=3, X=X_future)");
        }

        [Test]
        public void Mlforecast()
        {
            AssertCode(
                @"
import pandas as pd
import lightgbm as lgb

from mlforecast import MLForecast
from sklearn.linear_model import LinearRegression

def RunTest():
    df = pd.read_csv('https://datasets-nixtla.s3.amazonaws.com/air-passengers.csv', parse_dates=['ds'])
    mlf = MLForecast(
        models = [LinearRegression(), lgb.LGBMRegressor()],
        lags=[1, 12],
        freq = 'M'
    )
    mlf.fit(df)
    mlf.predict(12)");
        }

        [Test]
        public void Mapie()
        {
            AssertCode(
                @"
import numpy as np
from sklearn.linear_model import LinearRegression
from sklearn.datasets import make_regression
from sklearn.model_selection import train_test_split

from mapie.regression import MapieRegressor

def RunTest():
    X, y = make_regression(n_samples=500, n_features=1)
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.5)

    regressor = LinearRegression()

    mapie_regressor = MapieRegressor(estimator=regressor, method='plus', cv=5)

    mapie_regressor = mapie_regressor.fit(X_train, y_train)
    y_pred, y_pis = mapie_regressor.predict(X_test, alpha=[0.05, 0.32])");
        }

        [Test]
        public void H20()
        {
            AssertCode(
                @"
import h2o

def RunTest():
    h2o.init(ip = ""localhost"", port = 54321)");
        }

        [Test]
        public void Langchain()
        {
            AssertCode(
                @"
from langchain.prompts import PromptTemplate

def RunTest():
    prompt = PromptTemplate.from_template(""What is a good name for a company that makes {product}?"")
    prompt.format(product=""colorful socks"")");
        }

        [Test]
        public void Rbeast()
        {
            AssertCode(
                @"
import Rbeast as rb

def RunTest():
    (Nile, Year) = rb.load_example('nile')
    o = rb.beast(Nile, season = 'none')
    rb.plot(o)");
        }

        [Test, Explicit("Needs to be run by itself to avoid hanging")]
        public void Transformers()
        {
            AssertCode(
                @"
from transformers import pipeline

def RunTest():
    classifier = pipeline('sentiment-analysis')

    classifier('We are very happy to introduce pipeline to the transformers repository.')");
        }

        [Test]
        public void FixedEffectModel()
        {
            AssertCode(
                @"
import numpy as np
import pandas as pd

from fixedeffect.iv import ivgmm
from fixedeffect.utils.panel_dgp import gen_data

def RunTest():
    N = 100
    T = 10
    beta = [-3,1,2,3,4]
    ate = 1
    exp_date = 5
    df = gen_data(N, T, beta, ate, exp_date)
    formula = 'y ~ x_1|id+time|0|(x_2~x_3+x_4)'
    model_iv2sls = ivgmm(data_df = df, formula = formula)
    result = model_iv2sls.fit()
    result");
        }

        [Test]
        public void Iisignature()
        {
            AssertCode(
                @"
import iisignature
import numpy as np

def RunTest():
    path = np . random . uniform ( size =(20 ,3) )
    signature = iisignature . sig ( path ,4)
    s = iisignature . prepare (3 ,4)
    logsignature = iisignature . logsig ( path , s )");
        }

        [Test]
        public void PyStan()
        {
            AssertCode(
                @"
import stan

def RunTest():
    schools_code = """"""
    data {
      int<lower=0> J;         // number of schools
      array[J] real y;              // estimated treatment effects
      array[J] real<lower=0> sigma; // standard error of effect estimates
    }
    parameters {
      real mu;                // population treatment effect
      real<lower=0> tau;      // standard deviation in treatment effects
      vector[J] eta;          // unscaled deviation from mu by school
    }
    transformed parameters {
      vector[J] theta = mu + tau * eta;        // school treatment effects
    }
    model {
      target += normal_lpdf(eta | 0, 1);       // prior log-density
      target += normal_lpdf(y | theta, sigma); // log-likelihood
    }
    """"""

    schools_data = {""J"": 8,
                    ""y"": [28,  8, -3,  7, -1,  1, 18, 12],
                    ""sigma"": [15, 10, 16, 11,  9, 11, 10, 18]}

    posterior = stan.build(schools_code, data=schools_data)
    fit = posterior.sample(num_chains=4, num_samples=1000)
    eta = fit[""eta""]  # array with shape (8, 4000)
    df = fit.to_frame()  # pandas `DataFrame, requires pandas");
        }

        [Test]
        public void PyvinecopulibTest()
        {
            AssertCode(
                @"
import pyvinecopulib as pv
import numpy as np

def RunTest():
    np.random.seed(1234)  # seed for the random generator
    n = 1000  # number of observations
    d = 5  # the dimension
    mean = np.random.normal(size=d)  # mean vector
    cov = np.random.normal(size=(d, d))  # covariance matrix
    cov = np.dot(cov.transpose(), cov)  # make it non-negative definite
    x = np.random.multivariate_normal(mean, cov, n)

    # Transform copula data using the empirical distribution
    u = pv.to_pseudo_obs(x)

    # Fit a Gaussian vine
    # (i.e., properly specified since the data is multivariate normal)
    controls = pv.FitControlsVinecop(family_set=[pv.BicopFamily.gaussian])
    cop = pv.Vinecop(u, controls=controls)

    # Sample from the copula
    n_sim = 1000
    u_sim = cop.simulate(n_sim, seeds=[1, 2, 3, 4])

    # Transform back simulations to the original scale
    x_sim = np.asarray([np.quantile(x[:, i], u_sim[:, i]) for i in range(0, d)])

    # Both the mean and covariance matrix look ok!
    [mean, np.mean(x_sim, 1)]
    [cov, np.cov(x_sim)]");
        }

        [Test, Explicit("Needs to be run byitself to avoid exception on init: A colormap named \"cet_gray\" is already registered.")]
        public void HvplotTest()
        {
            AssertCode(
                @"
import numpy as np
import pandas as pd
import hvplot.pandas

def RunTest():
    index = pd.date_range('1/1/2000', periods=1000)
    df = pd.DataFrame(np.random.randn(1000, 4), index=index, columns=list('ABCD')).cumsum()

    df.head()
    pd.options.plotting.backend = 'holoviews'
    df.plot()");
        }

        [Test]
        public void StumpyTest()
        {
            AssertCode(
                @"
import stumpy
import numpy as np

def RunTest():
    your_time_series = np.random.rand(1000)
    window_size = 10  # Approximately, how many data points might be found in a pattern

    stumpy.stump(your_time_series, m=window_size)");
        }

        [Test]
        public void RiverTest()
        {
            AssertCode(
                @"
from river import datasets

def RunTest():
    datasets.Phishing()");
        }

        [Test]
        public void BokehTest()
        {
            AssertCode(
                @"
from bokeh.plotting import figure, output_file, show

def RunTest():
    # output to static HTML file
    output_file(""line.html"")

    p = figure(width=400, height=400)

    # add a circle renderer with a size, color, and alpha
    p.circle([1, 2, 3, 4, 5], [6, 7, 2, 4, 5], size=20, color=""navy"", alpha=0.5)

    # show the results
    show(p)");
        }

        [Test]
        public void LineProfilerTest()
        {
            AssertCode(
                @"
from line_profiler import LineProfiler
import random

def RunTest():
    def do_stuff(numbers):
        s = sum(numbers)
        l = [numbers[i]/43 for i in range(len(numbers))]
        m = ['hello'+str(numbers[i]) for i in range(len(numbers))]

    numbers = [random.randint(1,100) for i in range(1000)]
    lp = LineProfiler()
    lp_wrapper = lp(do_stuff)
    lp_wrapper(numbers)
    lp.print_stats()");
        }

        [Test]
        public void FuzzyCMeansTest()
        {
            AssertCode(
                @"
import numpy as np
from fcmeans import FCM
from matplotlib import pyplot as plt

def RunTest():
    n_samples = 3000

    X = np.concatenate((
        np.random.normal((-2, -2), size=(n_samples, 2)),
        np.random.normal((2, 2), size=(n_samples, 2))
    ))
    fcm = FCM(n_clusters=2)
    fcm.fit(X)
    # outputs
    fcm_centers = fcm.centers
    fcm.predict(X)");
        }

        [Test]
        public void MdptoolboxTest()
        {
            AssertCode(
                @"
import mdptoolbox.example

def RunTest():
    P, R = mdptoolbox.example.forest()
    vi = mdptoolbox.mdp.ValueIteration(P, R, 0.9)
    vi.run()
    vi.policy");
        }

        [Test]
        public void NumerapiTest()
        {
            AssertCode(
                @"
import numerapi

def RunTest():
    napi = numerapi.NumerAPI(verbosity=""warning"")
    napi.get_leaderboard()");
        }

        [Test]
        public void StockstatsTest()
        {
            AssertCode(
                @"
import pandas as pd
import stockstats

def RunTest():
    d = {'date': [ '20220901', '20220902' ], 'open': [ 1, 2 ], 'close': [ 1, 2 ],'high': [ 1, 2], 'low': [ 1, 2 ], 'volume': [ 1, 2 ] }
    df = pd.DataFrame(data=d)
    stock = stockstats.wrap(df)");
        }

        [Test]
        public void HurstTest()
        {
            AssertCode(
                @"
import numpy as np
import matplotlib.pyplot as plt
from hurst import compute_Hc, random_walk

def RunTest():
    # Use random_walk() function or generate a random walk series manually:
    # series = random_walk(99999, cumprod=True)
    np.random.seed(42)
    random_changes = 1. + np.random.randn(99999) / 1000.
    series = np.cumprod(random_changes)  # create a random walk from random changes

    # Evaluate Hurst equation
    H, c, data = compute_Hc(series, kind='price', simplified=True)");
        }

        [Test]
        public void PolarsTest()
        {
            AssertCode(
                @"
import polars as pl

def RunTest():
    df = pl.DataFrame({ ""A"": [1, 2, 3, 4, 5], ""fruits"": [""banana"", ""banana"", ""apple"", ""apple"", ""banana""], ""cars"": [""beetle"", ""audi"", ""beetle"", ""beetle"", ""beetle""], })
    df.sort(""fruits"")");
        }

        [Test, Explicit("Hangs if run along side the rest")]
        public void TensorflowProbabilityTest()
        {
            AssertCode(
                @"
import tensorflow as tf
import tensorflow_probability as tfp

def RunTest():
    # Pretend to load synthetic data set.
    features = tfp.distributions.Normal(loc=0., scale=1.).sample(int(100e3))
    labels = tfp.distributions.Bernoulli(logits=1.618 * features).sample()

    # Specify model.
    model = tfp.glm.Bernoulli()

    # Fit model given data.
    coeffs, linear_response, is_converged, num_iter = tfp.glm.fit(
        model_matrix=features[:, tf.newaxis],
        response=tf.cast(labels, dtype=tf.float32),
        model=model)");
        }

        [Test]
        public void MpmathTest()
        {
            AssertCode(
                @"
from mpmath import sin, cos

def RunTest():
    sin(1), cos(1)");
        }

        [Test]
        public void LimeTest()
        {
            AssertCode(
                @"
from __future__ import print_function
import sklearn
import sklearn.datasets
import sklearn.ensemble
import numpy as np
import lime
import lime.lime_tabular
np.random.seed(1)

def RunTest():
	iris = sklearn.datasets.load_iris()

	train, test, labels_train, labels_test = sklearn.model_selection.train_test_split(iris.data, iris.target, train_size=0.80)

	rf = sklearn.ensemble.RandomForestClassifier(n_estimators=500)
	rf.fit(train, labels_train)

	sklearn.metrics.accuracy_score(labels_test, rf.predict(test))
	explainer = lime.lime_tabular.LimeTabularExplainer(train, feature_names=iris.feature_names, class_names=iris.target_names, discretize_continuous=True)"
            );
        }

        [Test, Explicit("Should be run by itself to avoid matplotlib defaulting to use non existing latex")]
        public void ShapTest()
        {
            AssertCode(
                @"
import xgboost
import numpy as np
import shap

def RunTest():
	# simulate some binary data and a linear outcome with an interaction term
	# note we make the features in X perfectly independent of each other to make
	# it easy to solve for the exact SHAP values
	N = 2000
	X = np.zeros((N,5))
	X[:1000,0] = 1
	X[:500,1] = 1
	X[1000:1500,1] = 1
	X[:250,2] = 1
	X[500:750,2] = 1
	X[1000:1250,2] = 1
	X[1500:1750,2] = 1
	X[:,0:3] -= 0.5
	y = 2*X[:,0] - 3*X[:,1]

	Xd = xgboost.DMatrix(X, label=y)
	model = xgboost.train({
	    'eta':1, 'max_depth':3, 'base_score': 0, ""lambda"": 0
	}, Xd, 1)
	print(""Model error ="", np.linalg.norm(y-model.predict(Xd)))
	print(model.get_dump(with_stats=True)[0])

	# make sure the SHAP values add up to marginal predictions
	pred = model.predict(Xd, output_margin=True)
	explainer = shap.TreeExplainer(model)
	shap_values = explainer.shap_values(Xd)
	np.abs(shap_values.sum(1) + explainer.expected_value - pred).max()

	shap.summary_plot(shap_values, X)"
            );
        }

        [Test]
        public void MlxtendTest()
        {
            AssertCode(
                @"
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.gridspec as gridspec
import itertools
from sklearn.linear_model import LogisticRegression
from sklearn.svm import SVC
from sklearn.ensemble import RandomForestClassifier
from mlxtend.classifier import EnsembleVoteClassifier
from mlxtend.data import iris_data
from mlxtend.plotting import plot_decision_regions

def RunTest():
   # Initializing Classifiers
   clf1 = LogisticRegression(random_state=0)
   clf2 = RandomForestClassifier(random_state=0)
   clf3 = SVC(random_state=0, probability=True)
   eclf = EnsembleVoteClassifier(clfs=[clf1, clf2, clf3],
                                 weights=[2, 1, 1], voting='soft')
   # Loading some example data
   X, y = iris_data()
   X = X[:,[0, 2]]

   # Plotting Decision Regions
   gs = gridspec.GridSpec(2, 2)
   fig = plt.figure(figsize=(10, 8))

   labels = ['Logistic Regression',
             'Random Forest',
             'RBF kernel SVM',
             'Ensemble']

   for clf, lab, grd in zip([clf1, clf2, clf3, eclf],
                            labels,
                            itertools.product([0, 1],
                            repeat=2)):
       clf.fit(X, y)
       ax = plt.subplot(gs[grd[0], grd[1]])
       fig = plot_decision_regions(X=X, y=y,
                                   clf=clf, legend=2)
       plt.title(lab)

   plt.show()"
            );
        }

        [Test, Explicit("Hangs if run along side the rest")]
        public void IgniteTest()
        {
            AssertCode(
                $@"
import ignite

def RunTest():
    assert(ignite.__version__ == '0.4.13')"
            );
        }

        [Test, Explicit("Hangs if run along side the rest")]
        public void StellargraphTest()
        {
            AssertCode(
                $@"
import stellargraph

def RunTest():
    assert(stellargraph.__version__ == '1.2.1')"
            );
        }

        [Test, Explicit("Sometimes hangs when run along side the other tests")]
        public void TensorlyTest()
        {
            AssertCode(
                @"
import tensorly as tl
from tensorly import random

def RunTest():
	tensor = random.random_tensor((10, 10, 10))
	# This will be a NumPy array by default
	tl.set_backend('pytorch')
	# TensorLy now uses TensorLy for all operations

	tensor = random.random_tensor((10, 10, 10))
	# This will be a PyTorch array by default
	tl.max(tensor)
	tl.mean(tensor)
	tl.dot(tl.unfold(tensor, 0), tl.transpose(tl.unfold(tensor, 0)))"
            );
        }

        [Test]
        public void SpacyTest()
        {
            AssertCode(
                @"
import spacy
from spacy.lang.en.examples import sentences

def RunTest():
    nlp = spacy.load(""en_core_web_md"")
    doc = nlp(sentences[0])
    print(doc.text)"
            );
        }

        [Test]
        public void PyEMDTest()
        {
            AssertCode(
                @"
import numpy as np
import PyEMD

def RunTest():
    s = np.random.random(100)
    emd = PyEMD.EMD()
    IMFs = emd(s)"
            );
        }

        [Test]
        public void RipserTest()
        {
            AssertCode(
                @"
import numpy as np
import ripser
import persim
def RunTest():
    data = np.random.random((100,2))
    diagrams = ripser.ripser(data)['dgms']
    persim.plot_diagrams(diagrams, show=True)"
            );
        }

        [Test]
        public void AlphalensTest()
        {
            AssertCode(
                @"
import alphalens
import pandas
def RunTest():
    tickers = ['A', 'B', 'C', 'D', 'E', 'F']

    factor_groups = {'A': 1, 'B': 1, 'C': 1, 'D': 2, 'E': 2, 'F': 2}

    daily_rets = [1, 1, 2, 1, 1, 2]
    price_data = [[daily_rets[0]**i, daily_rets[1]**i, daily_rets[2]**i,
           daily_rets[3]**i, daily_rets[4]**i, daily_rets[5]**i]
          for i in range(1, 5)]  # 4 days

    start = '2015-1-11'
    factor_end = '2015-1-13'
    price_end = '2015-1-14'  # 1D fwd returns

    price_index = pandas.date_range(start=start, end=price_end)
    price_index.name = 'date'
    prices = pandas.DataFrame(index=price_index, columns=tickers, data=price_data)

    factor = 2
    factor_index = pandas.date_range(start=start, end=factor_end)
    factor_index.name = 'date'
    factor = pandas.DataFrame(index=factor_index, columns=tickers,
       data=factor).stack()

    # Ingest and format data
    factor_data = alphalens.utils.get_clean_factor_and_forward_returns(
        factor, prices,
        groupby=factor_groups,
        quantiles=None,
        bins=True,
        periods=(1,))"
            );
        }

        [Test]
        public void NumpyTest()
        {
            AssertCode(
                @"
import numpy
def RunTest():
    return numpy.pi"
            );
        }

        [Test]
        public void ScipyTest()
        {
            AssertCode(
                @"
import scipy
import numpy
def RunTest():
    return scipy.mean(numpy.array([1, 2, 3, 4, 5]))"
            );
        }

        [Test]
        public void SklearnTest()
        {
            AssertCode(
                @"
from sklearn.ensemble import RandomForestClassifier
def RunTest():
    return RandomForestClassifier()"
            );
        }

        [Test]
        public void CvxoptTest()
        {
            AssertCode(
                @"
import cvxopt
def RunTest():
    return cvxopt.matrix([1.0, 2.0, 3.0, 4.0, 5.0, 6.0], (2,3))"
            );
        }

        [Test]
        public void TalibTest()
        {
            AssertCode(
                @"
import numpy
import talib
def RunTest():
    return talib.SMA(numpy.random.random(100))"
            );
        }

        [Test]
        public void CvxpyTest()
        {
            AssertCode(
                @"
import numpy
import cvxpy
def RunTest():
    numpy.random.seed(1)
    n = 10
    mu = numpy.abs(numpy.random.randn(n, 1))
    Sigma = numpy.random.randn(n, n)
    Sigma = Sigma.T.dot(Sigma)

    w = cvxpy.Variable(n)
    gamma = cvxpy.Parameter(nonneg=True)
    ret = mu.T*w
    risk = cvxpy.quad_form(w, Sigma)
    return cvxpy.Problem(cvxpy.Maximize(ret - gamma*risk), [cvxpy.sum(w) == 1, w >= 0])"
            );
        }

        [Test]
        public void StatsmodelsTest()
        {
            AssertCode(
                @"
import numpy
import statsmodels.api as sm
def RunTest():
    nsample = 100
    x = numpy.linspace(0, 10, 100)
    X = numpy.column_stack((x, x**2))
    beta = numpy.array([1, 0.1, 10])
    e = numpy.random.normal(size=nsample)

    X = sm.add_constant(X)
    y = numpy.dot(X, beta) + e

    model = sm.OLS(y, X)
    results = model.fit()
    return results.summary()"
            );
        }

        [Test]
        public void PykalmanTest()
        {
            AssertCode(
                @"
import numpy
from pykalman import KalmanFilter
def RunTest():
    kf = KalmanFilter(transition_matrices = [[1, 1], [0, 1]], observation_matrices = [[0.1, 0.5], [-0.3, 0.0]])
    measurements = numpy.asarray([[1,0], [0,0], [0,1]])  # 3 observations
    kf = kf.em(measurements, n_iter=5)
    return kf.filter(measurements)"
            );
        }

        [Test]
        public void AesaraTest()
        {
            AssertCode(
                @"
import aesara
def RunTest():
    a = aesara.tensor.vector()          # declare variable
    out = a + a ** 10               # build symbolic expression
    f = aesara.function([a], out)   # compile function
    return f([0, 1, 2])"
            );
        }

        [Test]
        public void XgboostTest()
        {
            AssertCode(
                @"
import numpy
import xgboost
def RunTest():
    data = numpy.random.rand(5,10)            # 5 entities, each contains 10 features
    label = numpy.random.randint(2, size=5)   # binary target
    return xgboost.DMatrix( data, label=label)"
            );
        }

        [Test]
        public void ArchTest()
        {
            AssertCode(
                @"
import numpy
from arch import arch_model
def RunTest():
    r = numpy.array([0.945532630498276,
        0.614772790142383,
        0.834417758890680,
        0.862344782601800,
        0.555858715401929,
        0.641058419842652,
        0.720118656981704,
        0.643948007732270,
        0.138790608092353,
        0.279264178231250,
        0.993836948076485,
        0.531967023876420,
        0.964455754192395,
        0.873171802181126,
        0.937828816793698])

    garch11 = arch_model(r, p=1, q=1)
    res = garch11.fit(update_freq=10)
    return res.summary()"
            );
        }

        [Test, Explicit("Hangs if run along side the rest")]
        public void KerasTest()
        {
            AssertCode(
                @"
import numpy
from keras.models import Sequential
from keras.layers import Dense, Activation
def RunTest():
    # Initialize the constructor
    model = Sequential()

    # Add an input layer
    model.add(Dense(12, activation='relu', input_shape=(11,)))

    # Add one hidden layer
    model.add(Dense(8, activation='relu'))

    # Add an output layer
    model.add(Dense(1, activation='sigmoid'))
    return model"
            );
        }

        [Test, Explicit("Hangs if run along side the rest")]
        public void TensorflowTest()
        {
            AssertCode(
                @"
import tensorflow as tf
def RunTest():
    mnist = tf.keras.datasets.mnist

    (x_train, y_train), (x_test, y_test) = mnist.load_data()
    x_train, x_test = x_train / 255.0, x_test / 255.0

    model = tf.keras.models.Sequential([
        tf.keras.layers.Flatten(input_shape=(28, 28)),
        tf.keras.layers.Dense(128, activation='relu'),
        tf.keras.layers.Dropout(0.2),
        tf.keras.layers.Dense(10)
    ])
    model(x_train[:1]).numpy()"
            );
        }

        [Test]
        public void DeapTest()
        {
            AssertCode(
                @"
import numpy
from deap import algorithms, base, creator, tools
def RunTest():
    # onemax example evolves to print list of ones: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
    numpy.random.seed(1)
    def evalOneMax(individual):
        return sum(individual),

    creator.create('FitnessMax', base.Fitness, weights=(1.0,))
    creator.create('Individual', list, typecode = 'b', fitness = creator.FitnessMax)

    toolbox = base.Toolbox()
    toolbox.register('attr_bool', numpy.random.randint, 0, 1)
    toolbox.register('individual', tools.initRepeat, creator.Individual, toolbox.attr_bool, 10)
    toolbox.register('population', tools.initRepeat, list, toolbox.individual)
    toolbox.register('evaluate', evalOneMax)
    toolbox.register('mate', tools.cxTwoPoint)
    toolbox.register('mutate', tools.mutFlipBit, indpb = 0.05)
    toolbox.register('select', tools.selTournament, tournsize = 3)

    pop = toolbox.population(n = 50)
    hof = tools.HallOfFame(1)
    stats = tools.Statistics(lambda ind: ind.fitness.values)
    stats.register('avg', numpy.mean)
    stats.register('std', numpy.std)
    stats.register('min', numpy.min)
    stats.register('max', numpy.max)

    pop, log = algorithms.eaSimple(pop, toolbox, cxpb = 0.5, mutpb = 0.2, ngen = 30,
                                   stats = stats, halloffame = hof, verbose = False) # change to verbose=True to see evolution table
    return hof[0]"
            );
        }

        [Test]
        public void QuantlibTest()
        {
            AssertCode(
                @"
import QuantLib as ql
def RunTest():
    todaysDate = ql.Date(15, 1, 2015)
    ql.Settings.instance().evaluationDate = todaysDate
    spotDates = [ql.Date(15, 1, 2015), ql.Date(15, 7, 2015), ql.Date(15, 1, 2016)]
    spotRates = [0.0, 0.005, 0.007]
    dayCount = ql.Thirty360(ql.Thirty360.BondBasis)
    calendar = ql.UnitedStates(ql.UnitedStates.NYSE)
    interpolation = ql.Linear()
    compounding = ql.Compounded
    compoundingFrequency = ql.Annual
    spotCurve = ql.ZeroCurve(spotDates, spotRates, dayCount, calendar, interpolation,
                                compounding, compoundingFrequency)
    return ql.YieldTermStructureHandle(spotCurve)"
            );
        }

        [Test]
        public void CopulaTest()
        {
            AssertCode(
                @"
from copulas.univariate.gaussian import GaussianUnivariate
import pandas as pd
def RunTest():
    data=pd.DataFrame({'feature_01': [5.1, 4.9, 4.7, 4.6, 5.0]})
    feature1 = data['feature_01']
    gu = GaussianUnivariate()
    gu.fit(feature1)
    return gu"
            );
        }

        [Test]
        public void HmmlearnTest()
        {
            AssertCode(
                @"
import numpy as np
from hmmlearn import hmm
def RunTest():
    # Build an HMM instance and set parameters
    model = hmm.GaussianHMM(n_components=4, covariance_type='full')
    
    # Instead of fitting it from the data, we directly set the estimated
    # parameters, the means and covariance of the components
    model.startprob_ = np.array([0.6, 0.3, 0.1, 0.0])
    # The transition matrix, note that there are no transitions possible
    # between component 1 and 3
    model.transmat_ = np.array([[0.7, 0.2, 0.0, 0.1],
                               [0.3, 0.5, 0.2, 0.0],
                               [0.0, 0.3, 0.5, 0.2],
                               [0.2, 0.0, 0.2, 0.6]])
    # The means of each component
    model.means_ = np.array([[0.0,  0.0],
                             [0.0, 11.0],
                             [9.0, 10.0],
                             [11.0, -1.0]])
    # The covariance of each component
    model.covars_ = .5 * np.tile(np.identity(2), (4, 1, 1))

    # Generate samples
    return model.sample(500)"
            );
        }

        [Test]
        public void LightgbmTest()
        {
            AssertCode(
                @"
import lightgbm as lgb
import numpy as np
import pandas as pd
from scipy.special import expit
def RunTest():
    # Simulate some binary data with a single categorical and
    # single continuous predictor
    np.random.seed(0)
    N = 1000
    X = pd.DataFrame({
        'continuous': range(N),
        'categorical': np.repeat([0, 1, 2, 3, 4], N / 5)
    })
    CATEGORICAL_EFFECTS = [-1, -1, -2, -2, 2]
    LINEAR_TERM = np.array([
        -0.5 + 0.01 * X['continuous'][k]
        + CATEGORICAL_EFFECTS[X['categorical'][k]] for k in range(X.shape[0])
    ]) + np.random.normal(0, 1, X.shape[0])
    TRUE_PROB = expit(LINEAR_TERM)
    Y = np.random.binomial(1, TRUE_PROB, size=N)
    
    return {
        'X': X,
        'probability_labels': TRUE_PROB,
        'binary_labels': Y,
        'lgb_with_binary_labels': lgb.Dataset(X, Y),
        'lgb_with_probability_labels': lgb.Dataset(X, TRUE_PROB),
        }"
            );
        }

        [Test]
        public void FbProphetTest()
        {
            AssertCode(
                @"
import pandas as pd
from prophet import Prophet
def RunTest():
    df=pd.DataFrame({'ds': ['2007-12-10', '2007-12-11', '2007-12-12', '2007-12-13', '2007-12-14'], 'y': [9.590761, 8.519590, 8.183677, 8.072467, 7.893572]})
    m = Prophet()
    m.fit(df)
    future = m.make_future_dataframe(periods=365)
    return m.predict(future)"
            );
        }

        [Test]
        public void FastAiTest()
        {
            AssertCode(
                @"
from fastai.text import *
def RunTest():
    return 'Test is only importing the module, since available tests take too long'"
            );
        }

        [Test]
        public void PyramidArimaTest()
        {
            AssertCode(
                @"
import numpy as np
import pmdarima as pm
from pmdarima.datasets import load_wineind
def RunTest():
    # this is a dataset from R
    wineind = load_wineind().astype(np.float64)
    # fit stepwise auto-ARIMA
    stepwise_fit = pm.auto_arima(wineind, start_p=1, start_q=1,
                                 max_p=3, max_q=3, m=12,
                                 start_P=0, seasonal=True,
                                 d=1, D=1, trace=True,
                                 error_action='ignore',    # don't want to know if an order does not work
                                 suppress_warnings=True,   # don't want convergence warnings
                                 stepwise=True)            # set to stepwise

    return stepwise_fit.summary()"
            );
        }

        [Test]
        public void Ijson()
        {
            AssertCode(
                @"
import io
import ijson

def RunTest():
    parse_events = ijson.parse(io.BytesIO(b'[""skip"", {""a"": 1}, {""b"": 2}, {""c"": 3}]'))
    while True:
        prefix, event, value = next(parse_events)
        if value == ""skip"":
            break
    for obj in ijson.items(parse_events, 'item'):
        print(obj)");
        }

        [Test]
        public void MljarSupervised()
        {
            AssertCode(
                @"
import pandas as pd
from sklearn.model_selection import train_test_split
from supervised.automl import AutoML

def RunTest():
    df = pd.read_csv(
        ""https://raw.githubusercontent.com/pplonski/datasets-for-start/master/adult/data.csv"",
        skipinitialspace=True,
    )
    X_train, X_test, y_train, y_test = train_test_split(
        df[df.columns[:-1]], df[""income""], test_size=0.25
    )

    automl = AutoML()
    automl.fit(X_train, y_train)

    predictions = automl.predict(X_test)");
        }

        [Test]
        public void DmTree()
        {
            AssertCode(
                @"
import tree

def RunTest():
    structure = [[1], [[[2, 3]]], [4]]
    tree.flatten(structure)");
        }

        [Test]
        public void Ortools()
        {
            AssertCode(
                @"
from ortools.linear_solver import pywraplp

def RunTest():
	# Create the linear solver with the GLOP backend.
	solver = pywraplp.Solver.CreateSolver('GLOP')

	# Create the variables x and y.
	x = solver.NumVar(0, 1, 'x')
	y = solver.NumVar(0, 2, 'y')

	print('Number of variables =', solver.NumVariables())");
        }

        [Test, Explicit("Requires old version of TF, addons are winding down")]
        public void TensorflowAddons()
        {
            AssertCode(
                @"
import tensorflow as tf
import tensorflow_addons as tfa

def RunTest():
    train,test = tf.keras.datasets.mnist.load_data()
    x_train, y_train = train
    x_train = x_train[..., tf.newaxis] / 255.0");
        }

        [Test]
        public void Yellowbrick()
        {
            AssertCode(
                @"
from yellowbrick.features import ParallelCoordinates
from sklearn.datasets import make_classification

def RunTest():
    X, y = make_classification(n_samples=5000, n_features=2, n_informative=2,
                               n_redundant=0, n_repeated=0, n_classes=3,
                               n_clusters_per_class=1,
                               weights=[0.01, 0.05, 0.94],
                               class_sep=0.8, random_state=0)
    visualizer = ParallelCoordinates()
    visualizer.fit_transform(X, y)
    visualizer.show()");
        }

        [Test]
        public void Livelossplot()
        {
            AssertCode(
                @"
from sklearn import datasets
from sklearn.model_selection import train_test_split

import torch
from torch import nn, optim
from torch.utils.data import TensorDataset, DataLoader

import matplotlib.pyplot as plt
from matplotlib.colors import ListedColormap

from livelossplot import PlotLosses
from livelossplot.outputs import matplotlib_subplots

def RunTest():
	# try with make_moons
	X, y = datasets.make_circles(noise=0.2, factor=0.5, random_state=1)
	X_train, X_test, y_train, y_test = \
		train_test_split(X, y, test_size=.4, random_state=42)

	# plot them
	cm_bright = ListedColormap(['#FF0000', '#0000FF'])
	plt.scatter(X_train[:, 0], X_train[:, 1], c=y_train, cmap=cm_bright)
	plt.scatter(X_test[:, 0], X_test[:, 1], c=y_test, cmap=cm_bright, alpha=0.3)");
        }

        [Test]
        public void Gymnasium()
        {
            AssertCode(
                @"
import gymnasium as gym

def RunTest():
    env = gym.make(""CartPole-v1"")

    observation, info = env.reset(seed=42)
    action = env.action_space.sample()
    observation, reward, terminated, truncated, info = env.step(action)

    env.close()");
        }

        [Test]
        public void Interpret()
        {
            AssertCode(
                @"
import pandas as pd
from sklearn.model_selection import train_test_split
from interpret.glassbox import ExplainableBoostingClassifier
from io import StringIO

def RunTest():
    csv = StringIO(""39, State-gov, 77516, Bachelors, 13, Never-married, Adm-clerical, Not-in-family, White, Male, 2174, 0, 40, United-States, <=50K\n""
        + ""50, Self-emp-not-inc, 83311, Bachelors, 13, Married-civ-spouse, Exec-managerial, Husband, White, Male, 0, 0, 13, United-States, <=50K\n""
        + ""38, Private, 215646, HS-grad, 9, Divorced, Handlers-cleaners, Not-in-family, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""53, Private, 234721, 11th, 7, Married-civ-spouse, Handlers-cleaners, Husband, Black, Male, 0, 0, 40, United-States, <=50K\n""
        + ""28, Private, 338409, Bachelors, 13, Married-civ-spouse, Prof-specialty, Wife, Black, Female, 0, 0, 40, Cuba, <=50K\n""
        + ""37, Private, 284582, Masters, 14, Married-civ-spouse, Exec-managerial, Wife, White, Female, 0, 0, 40, United-States, <=50K\n""
        + ""49, Private, 160187, 9th, 5, Married-spouse-absent, Other-service, Not-in-family, Black, Female, 0, 0, 16, Jamaica, <=50K\n""
        + ""52, Self-emp-not-inc, 209642, HS-grad, 9, Married-civ-spouse, Exec-managerial, Husband, White, Male, 0, 0, 45, United-States, >50K\n""
        + ""31, Private, 45781, Masters, 14, Never-married, Prof-specialty, Not-in-family, White, Female, 14084, 0, 50, United-States, >50K\n""
        + ""42, Private, 159449, Bachelors, 13, Married-civ-spouse, Exec-managerial, Husband, White, Male, 5178, 0, 40, United-States, >50K\n""
        + ""37, Private, 280464, Some-college, 10, Married-civ-spouse, Exec-managerial, Husband, Black, Male, 0, 0, 80, United-States, >50K\n""
        + ""30, State-gov, 141297, Bachelors, 13, Married-civ-spouse, Prof-specialty, Husband, Asian-Pac-Islander, Male, 0, 0, 40, India, >50K\n""
        + ""23, Private, 122272, Bachelors, 13, Never-married, Adm-clerical, Own-child, White, Female, 0, 0, 30, United-States, <=50K\n""
        + ""32, Private, 205019, Assoc-acdm, 12, Never-married, Sales, Not-in-family, Black, Male, 0, 0, 50, United-States, <=50K\n""
        + ""40, Private, 121772, Assoc-voc, 11, Married-civ-spouse, Craft-repair, Husband, Asian-Pac-Islander, Male, 0, 0, 40, ?, >50K\n""
        + ""34, Private, 245487, 7th-8th, 4, Married-civ-spouse, Transport-moving, Husband, Amer-Indian-Eskimo, Male, 0, 0, 45, Mexico, <=50K\n""
        + ""25, Self-emp-not-inc, 176756, HS-grad, 9, Never-married, Farming-fishing, Own-child, White, Male, 0, 0, 35, United-States, <=50K\n""
        + ""32, Private, 186824, HS-grad, 9, Never-married, Machine-op-inspct, Unmarried, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""38, Private, 28887, 11th, 7, Married-civ-spouse, Sales, Husband, White, Male, 0, 0, 50, United-States, <=50K\n""
        + ""43, Self-emp-not-inc, 292175, Masters, 14, Divorced, Exec-managerial, Unmarried, White, Female, 0, 0, 45, United-States, >50K\n""
        + ""40, Private, 193524, Doctorate, 16, Married-civ-spouse, Prof-specialty, Husband, White, Male, 0, 0, 60, United-States, >50K\n""
        + ""54, Private, 302146, HS-grad, 9, Separated, Other-service, Unmarried, Black, Female, 0, 0, 20, United-States, <=50K\n""
        + ""35, Federal-gov, 76845, 9th, 5, Married-civ-spouse, Farming-fishing, Husband, Black, Male, 0, 0, 40, United-States, <=50K\n""
        + ""43, Private, 117037, 11th, 7, Married-civ-spouse, Transport-moving, Husband, White, Male, 0, 2042, 40, United-States, <=50K\n""
        + ""59, Private, 109015, HS-grad, 9, Divorced, Tech-support, Unmarried, White, Female, 0, 0, 40, United-States, <=50K\n""
        + ""56, Local-gov, 216851, Bachelors, 13, Married-civ-spouse, Tech-support, Husband, White, Male, 0, 0, 40, United-States, >50K\n""
        + ""19, Private, 168294, HS-grad, 9, Never-married, Craft-repair, Own-child, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""54, ?, 180211, Some-college, 10, Married-civ-spouse, ?, Husband, Asian-Pac-Islander, Male, 0, 0, 60, South, >50K\n""
        + ""39, Private, 367260, HS-grad, 9, Divorced, Exec-managerial, Not-in-family, White, Male, 0, 0, 80, United-States, <=50K\n""
        + ""49, Private, 193366, HS-grad, 9, Married-civ-spouse, Craft-repair, Husband, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""23, Local-gov, 190709, Assoc-acdm, 12, Never-married, Protective-serv, Not-in-family, White, Male, 0, 0, 52, United-States, <=50K\n""
        + ""20, Private, 266015, Some-college, 10, Never-married, Sales, Own-child, Black, Male, 0, 0, 44, United-States, <=50K\n""
        + ""45, Private, 386940, Bachelors, 13, Divorced, Exec-managerial, Own-child, White, Male, 0, 1408, 40, United-States, <=50K\n""
        + ""30, Federal-gov, 59951, Some-college, 10, Married-civ-spouse, Adm-clerical, Own-child, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""22, State-gov, 311512, Some-college, 10, Married-civ-spouse, Other-service, Husband, Black, Male, 0, 0, 15, United-States, <=50K\n""
        + ""48, Private, 242406, 11th, 7, Never-married, Machine-op-inspct, Unmarried, White, Male, 0, 0, 40, Puerto-Rico, <=50K\n""
        + ""21, Private, 197200, Some-college, 10, Never-married, Machine-op-inspct, Own-child, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""19, Private, 544091, HS-grad, 9, Married-AF-spouse, Adm-clerical, Wife, White, Female, 0, 0, 25, United-States, <=50K\n""
        + ""31, Private, 84154, Some-college, 10, Married-civ-spouse, Sales, Husband, White, Male, 0, 0, 38, ?, >50K\n""
        + ""48, Self-emp-not-inc, 265477, Assoc-acdm, 12, Married-civ-spouse, Prof-specialty, Husband, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""31, Private, 507875, 9th, 5, Married-civ-spouse, Machine-op-inspct, Husband, White, Male, 0, 0, 43, United-States, <=50K\n""
        + ""53, Self-emp-not-inc, 88506, Bachelors, 13, Married-civ-spouse, Prof-specialty, Husband, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""24, Private, 172987, Bachelors, 13, Married-civ-spouse, Tech-support, Husband, White, Male, 0, 0, 50, United-States, <=50K\n""
        + ""49, Private, 94638, HS-grad, 9, Separated, Adm-clerical, Unmarried, White, Female, 0, 0, 40, United-States, <=50K\n""
        + ""25, Private, 289980, HS-grad, 9, Never-married, Handlers-cleaners, Not-in-family, White, Male, 0, 0, 35, United-States, <=50K\n""
        + ""57, Federal-gov, 337895, Bachelors, 13, Married-civ-spouse, Prof-specialty, Husband, Black, Male, 0, 0, 40, United-States, >50K\n""
        + ""53, Private, 144361, HS-grad, 9, Married-civ-spouse, Machine-op-inspct, Husband, White, Male, 0, 0, 38, United-States, <=50K\n""
        + ""44, Private, 128354, Masters, 14, Divorced, Exec-managerial, Unmarried, White, Female, 0, 0, 40, United-States, <=50K\n""
        + ""41, State-gov, 101603, Assoc-voc, 11, Married-civ-spouse, Craft-repair, Husband, White, Male, 0, 0, 40, United-States, <=50K\n""
        + ""29, Private, 271466, Assoc-voc, 11, Never-married, Prof-specialty, Not-in-family, White, Male, 0, 0, 43, United-States, <=50K"")

    df = pd.read_csv(csv, header=None)
    df.columns = [
        ""Age"", ""WorkClass"", ""fnlwgt"", ""Education"", ""EducationNum"",
        ""MaritalStatus"", ""Occupation"", ""Relationship"", ""Race"", ""Gender"",
        ""CapitalGain"", ""CapitalLoss"", ""HoursPerWeek"", ""NativeCountry"", ""Income""
    ]
    train_cols = df.columns[0:-1]
    label = df.columns[-1]
    X = df[train_cols]
    y = df[label]

    seed = 1
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.20, random_state=seed)

    ebm = ExplainableBoostingClassifier(random_state=seed)
    ebm.fit(X_train, y_train)");
        }

        [Test]
        public void Doubleml()
        {
            AssertCode(
                @"
import numpy as np
from doubleml.datasets import make_plr_CCDDHNR2018

def RunTest():
    np.random.seed(1234)

    n_rep = 1000
    n_obs = 500
    n_vars = 20
    alpha = 0.5
    data = list()

    for i_rep in range(n_rep):
        (x, y, d) = make_plr_CCDDHNR2018(alpha=alpha, n_obs=n_obs, dim_x=n_vars, return_type='array')
        data.append((x, y, d))");
        }

        [Test]
        public void ImbalancedLearn()
        {
            AssertCode(
                @"
from sklearn.datasets import make_classification
from imblearn.over_sampling import RandomOverSampler
from collections import Counter

def RunTest():
    X, y = make_classification(n_samples=5000, n_features=2, n_informative=2,
                                n_redundant=0, n_repeated=0, n_classes=3,
                                n_clusters_per_class=1,
                                weights=[0.01, 0.05, 0.94],
                                class_sep=0.8, random_state=0)

    ros = RandomOverSampler(random_state=0)

    X_resampled, y_resampled = ros.fit_resample(X, y)

    print(sorted(Counter(y_resampled).items()))");
        }

        [Test, Explicit("Requires keras < 3")]
        public void ScikerasTest()
        {
            AssertCode(
                @"
import numpy as np
from sklearn.datasets import make_classification
from tensorflow import keras
from scikeras.wrappers import KerasClassifier

def RunTest():
    X, y = make_classification(1000, 20, n_informative=10, random_state=0)
    X = X.astype(np.float32)
    y = y.astype(np.int64)

    def get_model(hidden_layer_dim, meta):
        # note that meta is a special argument that will be
        # handed a dict containing input metadata
        n_features_in_ = meta[""n_features_in_""]
        X_shape_ = meta[""X_shape_""]
        n_classes_ = meta[""n_classes_""]

        model = keras.models.Sequential()
        model.add(keras.layers.Dense(n_features_in_, input_shape=X_shape_[1:]))
        model.add(keras.layers.Activation(""relu""))
        model.add(keras.layers.Dense(hidden_layer_dim))
        model.add(keras.layers.Activation(""relu""))
        model.add(keras.layers.Dense(n_classes_))
        model.add(keras.layers.Activation(""softmax""))
        return model

    clf = KerasClassifier(
        get_model,
        loss=""sparse_categorical_crossentropy"",
        hidden_layer_dim=100,
    )

    clf.fit(X, y)
    y_proba = clf.predict_proba(X)");
        }

        [Test]
        public void Lazypredict()
        {
            AssertCode(
                @"
from lazypredict.Supervised import LazyClassifier
from sklearn.datasets import load_breast_cancer
from sklearn.model_selection import train_test_split

def RunTest():
    data = load_breast_cancer()
    X = data.data
    y= data.target

    X_train, X_test, y_train, y_test = train_test_split(X, y,test_size=.5,random_state =123)

    clf = LazyClassifier(verbose=0,ignore_warnings=True, custom_metric=None)
    models,predictions = clf.fit(X_train, X_test, y_train, y_test)");
        }

        [Test]
        public void Darts()
        {
            AssertCode(
                @"
from darts.datasets import ETTh2Dataset
from darts.ad import KMeansScorer

def RunTest():
    series = ETTh2Dataset().load()[:10000][[""MUFL"", ""LULL""]]
    train, val = series.split_before(0.6)
    scorer = KMeansScorer(k=2, window=5)
    scorer.fit(train)
    anom_score = scorer.score(val)");
        }

        [Test]
        public void Fastparquet()
        {
            AssertCode(
                @"
from fastparquet import write
import pandas as pd

def RunTest():
    d = {'date': [ '20220901', '20220902' ], 'open': [ 1, 2 ], 'close': [ 1, 2 ],'high': [ 1, 2], 'low': [ 1, 2 ], 'volume': [ 1, 2 ] }
    df = pd.DataFrame(data=d)
    write('outfile.parq', df)");
        }

        [Test]
        public void Dimod()
        {
            AssertCode(
                @"
import dimod

def RunTest():
    bqm = dimod.BinaryQuadraticModel({0: -1, 1: 1}, {(0, 1): 2}, 0.0, dimod.BINARY)

    sampleset = dimod.ExactSolver().sample(bqm)
    return sampleset");
        }

        [Test]
        public void DwaveSamplers()
        {
            AssertCode(
                @"
from dwave.samplers import PlanarGraphSolver

def RunTest():
    solver = PlanarGraphSolver()");
        }

        [Test]
        public void Statemachine()
        {
            AssertCode(
                @"
from statemachine import StateMachine, State

def RunTest():
    class StateObject(StateMachine):
        aState = State(""A"", initial = True)
        bState = State(""B"")

        transitionA = aState.to(bState)
        transitionB = bState.to(aState)

    instance = StateObject()");
        }

        [Test]
        public void pymannkendall()
        {
            AssertCode(
                @"
import numpy as np
import pymannkendall as mk

def RunTest():
    # Data generation for analysis
    data = np.random.rand(360,1)

    result = mk.original_test(data)
    return result");
        }

        [Test]
        public void Pyomo()
        {
            AssertCode(
                @"
from pyomo.environ import *

def RunTest():
	V = 40     # liters
	kA = 0.5   # 1/min
	kB = 0.1   # l/min
	CAf = 2.0  # moles/liter

	# create a model instance
	model = ConcreteModel()

	# create x and y variables in the model
	model.q = Var()

	# add a model objective
	model.objective = Objective(expr = model.q*V*kA*CAf/(model.q + V*kB)/(model.q + V*kA), sense=maximize)

	# compute a solution using ipopt for nonlinear optimization
	results = SolverFactory('ipopt').solve(model)

	# print solutions
	qmax = model.q()
	CBmax = model.objective()
	print('\nFlowrate at maximum CB = ', qmax, 'liters per minute.')
	print('\nMaximum CB =', CBmax, 'moles per liter.')
	print('\nProductivity = ', qmax*CBmax, 'moles per minute.')");
        }

        [Test]
        public void Gpflow()
        {
            AssertCode(
                @"
import gpflow
import numpy as np
import matplotlib

def RunTest():
    X = np.array(
        [
            [0.865], [0.666], [0.804], [0.771], [0.147], [0.866], [0.007], [0.026],
            [0.171], [0.889], [0.243], [0.028],
        ]
    )
    Y = np.array(
        [
            [1.57], [3.48], [3.12], [3.91], [3.07], [1.35], [3.80], [3.82], [3.49],
            [1.30], [4.00], [3.82],
        ]
    )

    model = gpflow.models.GPR((X, Y), kernel=gpflow.kernels.SquaredExponential())
    opt = gpflow.optimizers.Scipy()
    opt.minimize(model.training_loss, model.trainable_variables)

    Xnew = np.array([[0.5]])
    model.predict_f(Xnew)");
        }

        [Test, Explicit("Sometimes hangs when run along side the other tests")]
        public void StableBaselinesTest()
        {
            AssertCode(
                @"
from stable_baselines3 import PPO
from stable_baselines3.common.env_util import make_vec_env

def RunTest():
    env = make_vec_env(""CartPole-v1"", n_envs=1)

    model = PPO(""MlpPolicy"", env, verbose=1)
    model.learn(total_timesteps=500)");
        }

        [Test]
        public void GensimTest()
        {
            AssertCode(
                @"
from gensim import models

def RunTest():
    # https://radimrehurek.com/gensim/tutorial.html
    corpus = [[(0, 1.0), (1, 1.0), (2, 1.0)],
              [(2, 1.0), (3, 1.0), (4, 1.0), (5, 1.0), (6, 1.0), (8, 1.0)],
              [(1, 1.0), (3, 1.0), (4, 1.0), (7, 1.0)],
              [(0, 1.0), (4, 2.0), (7, 1.0)],
              [(3, 1.0), (5, 1.0), (6, 1.0)],
              [(9, 1.0)],
              [(9, 1.0), (10, 1.0)],
              [(9, 1.0), (10, 1.0), (11, 1.0)],
              [(8, 1.0), (10, 1.0), (11, 1.0)]]

    tfidf = models.TfidfModel(corpus)
    vec = [(0, 1), (4, 1)]
    return f'{tfidf[vec]}'"
            );
        }

        [Test]
        public void ScikitOptimizeTest()
        {
            AssertCode(
                @"
import numpy as np
from skopt import gp_minimize

def f(x):
    return (np.sin(5 * x[0]) * (1 - np.tanh(x[0] ** 2)) * np.random.randn() * 0.1)

def RunTest():
    res = gp_minimize(f, [(-2.0, 2.0)])
    return f'Test passed: {res}'"
            );
        }

        [Test]
        public void CremeTest()
        {
            AssertCode(
                @"
from creme import datasets

def RunTest():
    X_y = datasets.Bikes()
    x, y = next(iter(X_y))
    return f'Number of bikes: {y}'"
            );
        }

        [Test]
        public void NltkTest()
        {
            AssertCode(
                @"
import nltk.data

def RunTest():
    text = '''
    Punkt knows that the periods in Mr. Smith and Johann S. Bach
    do not mark sentence boundaries.  And sometimes sentences
    can start with non-capitalized words.  i is a good variable
    name.
    '''
    sent_detector = nltk.data.load('tokenizers/punkt/english.pickle')
    return '\n-----\n'.join(sent_detector.tokenize(text.strip()))"
            );
        }

        [Test]
        public void NltkVaderTest()
        {
            AssertCode(
                @"
from nltk.sentiment.vader import SentimentIntensityAnalyzer
from nltk import tokenize

def RunTest():
    sentences = [
        'VADER is smart, handsome, and funny.', # positive sentence example...    'VADER is smart, handsome, and funny!', # punctuation emphasis handled correctly (sentiment intensity adjusted)
        'VADER is very smart, handsome, and funny.',  # booster words handled correctly (sentiment intensity adjusted)
        'VADER is VERY SMART, handsome, and FUNNY.',  # emphasis for ALLCAPS handled
        'VADER is VERY SMART, handsome, and FUNNY!!!',# combination of signals - VADER appropriately adjusts intensity
        'VADER is VERY SMART, really handsome, and INCREDIBLY FUNNY!!!',# booster words & punctuation make this close to ceiling for score
        'The book was good.',         # positive sentence
        'The book was kind of good.', # qualified positive sentence is handled correctly (intensity adjusted)
        'The plot was good, but the characters are uncompelling and the dialog is not great.', # mixed negation sentence
        'A really bad, horrible book.',       # negative sentence with booster words
        'At least it is not a horrible book.', # negated negative sentence with contraction
        ':) and :D',     # emoticons handled
        '',              # an empty string is correctly handled
        'Today sux',     #  negative slang handled
        'Today sux!',    #  negative slang with punctuation emphasis handled
        'Today SUX!',    #  negative slang with capitalization emphasis
        'Today kinda sux! But I will get by, lol' # mixed sentiment example with slang and constrastive conjunction 'but'
    ]
    paragraph = 'It was one of the worst movies I have seen, despite good reviews. \
        Unbelievably bad acting!! Poor direction.VERY poor production. \
        The movie was bad.Very bad movie.VERY bad movie.VERY BAD movie.VERY BAD movie!'

    lines_list = tokenize.sent_tokenize(paragraph)
    sentences.extend(lines_list)

    sid = SentimentIntensityAnalyzer()
    for sentence in sentences:
        ss = sid.polarity_scores(sentence)

    return f'{sid}'"
            );
        }

        [Test, Explicit("Requires mlfinlab installed")]
        public void MlfinlabTest()
        {
            AssertCode(
                @"
from mlfinlab.portfolio_optimization.hrp import HierarchicalRiskParity
from mlfinlab.portfolio_optimization.mean_variance import MeanVarianceOptimisation
import numpy as np
import pandas as pd
import os

def RunTest():
# Read in data
    data_file = os.getcwd() + '/TestData/stock_prices.csv'
    stock_prices = pd.read_csv(data_file, parse_dates=True, index_col='Date') # The date column may be named differently for your input.

    # Compute HRP weights
    hrp = HierarchicalRiskParity()
    hrp.allocate(asset_prices=stock_prices, resample_by='B')
    hrp_weights = hrp.weights.sort_values(by=0, ascending=False, axis=1)

    # Compute IVP weights
    mvo = MeanVarianceOptimisation()
    mvo.allocate(asset_prices=stock_prices, solution='inverse_variance', resample_by='B')
    ivp_weights = mvo.weights.sort_values(by=0, ascending=False, axis=1)
    
    return f'HRP: {hrp_weights} IVP: {ivp_weights}'"
            );
        }

        [Test]
        public void JaxTest()
        {
            AssertCode(
                @"
from jax import *
import jax.numpy as jnp

def predict(params, inputs):
  for W, b in params:
    outputs = jnp.dot(inputs, W) + b
    inputs = jnp.tanh(outputs)
  return outputs

def logprob_fun(params, inputs, targets):
  preds = predict(params, inputs)
  return jnp.sum((preds - targets)**2)

def RunTest():
    grad_fun = jit(grad(logprob_fun))  # compiled gradient evaluation function
    return jit(vmap(grad_fun, in_axes=(None, 0, 0)))  # fast per-example grads"
            );
        }

        [Test, Explicit("Has issues when run along side the other tests. random.PRNGKey call hangs")]
        public void NeuralTangentsTest()
        {
            AssertCode(
                @"
from jax import *
import neural_tangents as nt
from neural_tangents import *

def RunTest():
    key = random.PRNGKey(1)
    key1, key2 = random.split(key, 2)
    x_train = random.normal(key1, (20, 32, 32, 3))
    y_train = random.uniform(key1, (20, 10))
    x_test = random.normal(key2, (5, 32, 32, 3))

    init_fn, apply_fn, kernel_fn = stax.serial(
        stax.Conv(128, (3, 3)),
        stax.Relu(),
        stax.Conv(256, (3, 3)),
        stax.Relu(),
        stax.Conv(512, (3, 3)),
        stax.Flatten(),
        stax.Dense(10)
    )

    predict_fn = nt.predict.gradient_descent_mse_ensemble(kernel_fn, x_train, y_train)
    # (5, 10) np.ndarray NNGP test prediction
    predict_fn(x_test=x_test, get='nngp')"
            );
        }


        [Test]
        public void SmmTest()
        {
            AssertCode(
                @"
import ssm

def RunTest():
    T = 100  # number of time bins
    K = 5    # number of discrete states
    D = 2    # dimension of the observations

    # make an hmm and sample from it
    hmm = ssm.HMM(K, D, observations='gaussian')
    z, y = hmm.sample(T)
    test_hmm = ssm.HMM(K, D, observations='gaussian')
    test_hmm.fit(y)
    return test_hmm.most_likely_states(y)"
            );
        }

        [Test]
        public void RiskparityportfolioTest()
        {
            AssertCode(
                @"
import riskparityportfolio as rp
import numpy as np

def RunTest():
    Sigma = np.vstack((np.array((1.0000, 0.0015, -0.0119)),
                   np.array((0.0015, 1.0000, -0.0308)),
                   np.array((-0.0119, -0.0308, 1.0000))))
    b = np.array((0.1594, 0.0126, 0.8280))
    w = rp.vanilla.design(Sigma, b)
    rc = w @ (Sigma * w)
    return rc/np.sum(rc)"
            );
        }

        [Test]
        public void PyrbTest()
        {
            AssertCode(
                @"
import pandas as pd
import numpy as np
from pyrb import ConstrainedRiskBudgeting

def RunTest():
    vol = [0.05,0.05,0.07,0.1,0.15,0.15,0.15,0.18]
    cor = np.array([[100,  80,  60, -20, -10, -20, -20, -20],
               [ 80, 100,  40, -20, -20, -10, -20, -20],
               [ 60,  40, 100,  50,  30,  20,  20,  30],
               [-20, -20,  50, 100,  60,  60,  50,  60],
               [-10, -20,  30,  60, 100,  90,  70,  70],
               [-20, -10,  20,  60,  90, 100,  60,  70],
               [-20, -20,  20,  50,  70,  60, 100,  70],
               [-20, -20,  30,  60,  70,  70,  70, 100]])/100
    cov = np.outer(vol,vol)*cor
    C = None
    d = None

    CRB = ConstrainedRiskBudgeting(cov,C=C,d=d)
    CRB.solve()
    return CRB"
            );
        }

        [Test]
        public void CopulaeTest()
        {
            AssertCode(
                @"
from copulae import NormalCopula
import numpy as np

def RunTest():
    np.random.seed(8)
    data = np.random.normal(size=(300, 8))
    cop = NormalCopula(8)
    cop.fit(data)

    cop.random(10)  # simulate random number

    # getting parameters
    p = cop.params
    # cop.params = ...  # you can override parameters too, even after it's fitted!  

    # get a summary of the copula. If it's fitted, fit details will be present too
    return cop.summary()"
            );
        }
        [Test]
        public void SanityClrInstallation()
        {
            AssertCode(
                @"
from os import walk
import setuptools as _

def RunTest():
    try:
        import clr
        clr.AddReference()
        print('No clr errors')
        #Checks complete
    except: #isolate error cause
        try:
            import clr
            print('clr exists') #Module exists
            try:
                f = []
                for (dirpath, dirnames, filenames) in walk(print(clr.__path__)):
                    f.extend(filenames)
                    break
                return(f.values['style_builder.py']) #If this is reached, likely due to an issue with this file itself
            except:
                print('no style_builder') #pythonnet install error, most likely

        except:
            print('clr does not exist') #Only remaining cause"
            );
        }

        [Test, Explicit("Sometimes hangs when run along side the other tests")]
        public void AxPlatformTest()
        {
            AssertCode(@"
from ax import optimize

def RunTest():
    best_parameters, best_values, experiment, model = optimize(
            parameters=[
              {
                ""name"": ""x1"",
                ""type"": ""range"",
                ""bounds"": [-10.0, 10.0],
              },
              {
                ""name"": ""x2"",
                ""type"": ""range"",
                ""bounds"": [-10.0, 10.0],
              },
            ],
            # Booth function
            evaluation_function=lambda p: (p[""x1""] + 2*p[""x2""] - 7)**2 + (2*p[""x1""] + p[""x2""] - 5)**2,
            minimize=True,
        )
");
        }

        [Test]
        public void RiskfolioLibTest()
        {
            AssertCode(@"
import riskfolio as rp
import pandas as pd

def RunTest():
	# Data
	date_index = pd.DatetimeIndex(data=['2020-06-15', '2020-06-15', '2020-06-15'])
	d = {'AAPL': [10, 22, 11], 'AMC': [21,  13, 45]}
	df = pd.DataFrame(data=d).set_index(date_index)
	df = df.pct_change().dropna()

	# Building the portfolio object
	port = rp.Portfolio(returns=df)

	method_mu='hist' # Method to estimate expected returns based on historical data.
	method_cov='hist' # Method to estimate covariance matrix based on historical data.

	port.assets_stats(method_mu=method_mu, method_cov=method_cov, d=0.94)

	# Estimate optimal portfolio:

	model='Classic' # Could be Classic (historical), BL (Black Litterman) or FM (Factor Model)
	rm = 'MV' # Risk measure used, this time will be variance
	obj = 'Sharpe' # Objective function, could be MinRisk, MaxRet, Utility or Sharpe
	hist = True # Use historical scenarios for risk measures that depend on scenarios
	rf = 0 # Risk free rate
	l = 0 # Risk aversion factor, only useful when obj is 'Utility'

	w = port.optimization(model=model, rm=rm, obj=obj, rf=rf, l=l, hist=hist)

	w.T");
        }

        /// <summary>
        /// Simple test for modules that don't have short test example
        /// </summary>
        /// <param name="module">The module we are testing</param>
        /// <param name="version">The module version</param>
        [TestCase("pulp", "2.8.0", "VERSION")]
        [TestCase("pymc", "5.10.4", "__version__")]
        [TestCase("pypfopt", "pypfopt", "__name__")]
        [TestCase("wrapt", "1.16.0", "__version__")]
        [TestCase("tslearn", "0.6.3", "__version__")]
        [TestCase("tweepy", "4.14.0", "__version__")]
        [TestCase("pywt", "1.5.0", "__version__")]
        [TestCase("umap", "0.5.5", "__version__")]
        [TestCase("dtw", "1.3.1", "__version__")]
        [TestCase("mplfinance", "0.12.10b0", "__version__")]
        [TestCase("cufflinks", "0.17.3", "__version__")]
        [TestCase("ipywidgets", "8.1.2", "__version__")]
        [TestCase("astropy", "6.0.0", "__version__")]
        [TestCase("gluonts", "0.14.4", "__version__")]
        [TestCase("gplearn", "0.4.2", "__version__")]
        [TestCase("featuretools", "1.30.0", "__version__")]
        [TestCase("pennylane", "0.35.1", "version()")]
        [TestCase("pyfolio", "0.9.5", "__version__")]
        [TestCase("altair", "5.2.0", "__version__")]
        [TestCase("modin", "0.26.1", "__version__")]
        [TestCase("persim", "0.3.5", "__version__")]
        [TestCase("pydmd", "1.0.0", "__version__")]
        [TestCase("pandas_ta", "0.3.14b0", "__version__")]
        [TestCase("tensortrade", "1.0.3", "__version__")]
        [TestCase("quantstats", "0.0.62", "__version__")]
        [TestCase("panel", "1.3.8", "__version__")]
        [TestCase("pyheat", "pyheat", "__name__")]
        [TestCase("tensorflow_decision_forests", "1.9.0", "__version__")]
        [TestCase("pomegranate", "1.0.4", "__version__")]
        [TestCase("cv2", "4.9.0", "__version__")]
        [TestCase("ot", "0.9.3", "__version__")]
        [TestCase("datasets", "2.17.1", "__version__")]
        public void ModuleVersionTest(string module, string value, string attribute)
        {
            AssertCode(
                $@"
import {module}

def RunTest():
    assert({module}.{attribute} == '{value}')
    return 'Test passed, module exists'"
            );
        }

        private static void AssertCode(string code)
        {
            using (Py.GIL())
            {
                using dynamic module = PyModule.FromString(Guid.NewGuid().ToString(), code);
                Assert.DoesNotThrow(() =>
                {
                    var response = module.RunTest();
                    if(response != null)
                    {
                        response.Dispose();
                    }
                });
            }
        }
    }
}
