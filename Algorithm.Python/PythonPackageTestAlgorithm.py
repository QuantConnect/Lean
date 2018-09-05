# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QCAlgorithm import QCAlgorithm

# Libraries included with basic python install
from bisect import bisect
import cmath
import collections
import copy
import functools
import heapq
import itertools
import math
import operator
import pytz
import re
import time
import zlib

# Third party libraries added with pip
from sklearn.ensemble import RandomForestClassifier
import blaze   # includes sqlalchemy, odo
import numpy
import scipy
import cvxopt
import cvxpy
from pykalman import KalmanFilter
import statsmodels.api as sm
import talib
from copulalib.copulalib import Copula
import theano
import xgboost
from arch import arch_model
from keras.models import Sequential
from keras.layers import Dense, Activation
import tensorflow as tf
from deap import algorithms, base, creator, tools

### <summary>
### Demonstration of all the packages you can import with the QuantConnect/LEAN trading engine.s
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
class PythonPackageTestAlgorithm(QCAlgorithm):
    '''Algorithm to test third party libraries'''

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)   #Set Start Date
        self.SetStartDate(2013, 10, 7)  #Set End Date
        self.AddEquity("SPY", Resolution.Daily)

        # numpy test
        self.Log(f"numpy test >>> print numpy.pi: {numpy.pi}")

        # scipy test:
        self.Log(f"scipy test >>> print mean of 1 2 3 4 5: {scipy.mean(numpy.array([1, 2, 3, 4, 5]))}")

        #sklearn test
        self.Log(f"sklearn test >>> default RandomForestClassifier: {RandomForestClassifier()}")

        # cvxopt matrix test
        self.Log(f"cvxopt >>> {cvxopt.matrix([1.0, 2.0, 3.0, 4.0, 5.0, 6.0], (2,3))}")

        # talib test
        self.Log(f"talib test >>> {talib.SMA(numpy.random.random(100))}")

        # blaze test
        self.Log(blaze_test())

        # cvxpy test
        self.Log(cvxpy_test())

        # statsmodels test
        self.Log(statsmodels_test())

        # pykalman test
        self.Log(pykalman_test())

        # copulalib test
        self.Log(copulalib_test())

        # theano test
        self.Log(theano_test())

        # xgboost test
        self.Log(xgboost_test())

        # arch test
        self.Log(arch_test())

        # keras test
        self.Log(keras_test())

        # tensorflow test
        self.Log(tensorflow_test())

        # deap test
        self.Log(deap_test())

    def OnData(self, data): pass

def blaze_test():
    accounts = blaze.symbol('accounts', 'var * {id: int, name: string, amount: int}')
    deadbeats = accounts[accounts.amount < 0].name
    L = [[1, 'Alice',   100],
         [2, 'Bob',    -200],
         [3, 'Charlie', 300],
         [4, 'Denis',   400],
         [5, 'Edith',  -500]]
    return f"blaze test >>> {list(blaze.compute(deadbeats, L))}"

def grade(score, breakpoints=[60, 70, 80, 90], grades='FDCBA'):
    i = bisect(breakpoints, score)
    return grades[i]

def cvxpy_test():
    numpy.random.seed(1)
    n = 10
    mu = numpy.abs(numpy.random.randn(n, 1))
    Sigma = numpy.random.randn(n, n)
    Sigma = Sigma.T.dot(Sigma)

    w = cvxpy.Variable(n)
    gamma = cvxpy.Parameter(nonneg=True)
    ret = mu.T*w
    risk = cvxpy.quad_form(w, Sigma)
    result = cvxpy.Problem(cvxpy.Maximize(ret - gamma*risk),
                           [cvxpy.sum(w) == 1, w >= 0])
    return f"csvpy test >>> {result}" 

def statsmodels_test():
    nsample = 100
    x = numpy.linspace(0, 10, 100)
    X = numpy.column_stack((x, x**2))
    beta = numpy.array([1, 0.1, 10])
    e = numpy.random.normal(size=nsample)

    X = sm.add_constant(X)
    y = numpy.dot(X, beta) + e

    model = sm.OLS(y, X)
    results = model.fit()
    return f"statsmodels tests >>> {results.summary()}"

def pykalman_test():
    kf = KalmanFilter(transition_matrices = [[1, 1], [0, 1]], observation_matrices = [[0.1, 0.5], [-0.3, 0.0]])
    measurements = numpy.asarray([[1,0], [0,0], [0,1]])  # 3 observations
    kf = kf.em(measurements, n_iter=5)
    return f"pykalman test >>> {kf.filter(measurements)}"

def copulalib_test():
    x = numpy.random.normal(size=100)
    y = 2.5 * x + numpy.random.normal(size=100)

    #Make the instance of Copula class with x, y and clayton family::
    return f"copulalib test >>> {Copula(x, y, family='clayton')}"

def theano_test():
    a = theano.tensor.vector() # declare variable
    out = a + a ** 10               # build symbolic expression
    f = theano.function([a], out)   # compile function
    return f"theano test >>> {f([0, 1, 2])}"

def xgboost_test():
    data = numpy.random.rand(5,10) # 5 entities, each contains 10 features
    label = numpy.random.randint(2, size=5) # binary target
    return f"xgboost test >>> {xgboost.DMatrix( data, label=label)}"

def arch_test():
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
    return f"arch test >>> {res.summary()}"

def keras_test():
    # Initialize the constructor
    model = Sequential()

    # Add an input layer
    model.add(Dense(12, activation='relu', input_shape=(11,)))

    # Add one hidden layer
    model.add(Dense(8, activation='relu'))

    # Add an output layer
    model.add(Dense(1, activation='sigmoid'))

    return f"keras test >>> {model}"

def tensorflow_test():
    node1 = tf.constant(3.0, tf.float32)
    node2 = tf.constant(4.0) # also tf.float32 implicitly
    sess = tf.Session()
    node3 = tf.add(node1, node2)
    return f"tensorflow test >>> sess.run(node3): {sess.run(node3)}"

def deap_test():
    # onemax example evolves to print list of ones: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
    numpy.random.seed(1)
    def evalOneMax(individual):
        return sum(individual),

    creator.create("FitnessMax", base.Fitness, weights=(1.0,))
    creator.create("Individual", list, typecode='b', fitness=creator.FitnessMax)

    toolbox = base.Toolbox()
    toolbox.register("attr_bool", numpy.random.randint, 0, 1)
    toolbox.register("individual", tools.initRepeat, creator.Individual, toolbox.attr_bool, 10)
    toolbox.register("population", tools.initRepeat, list, toolbox.individual)
    toolbox.register("evaluate", evalOneMax)
    toolbox.register("mate", tools.cxTwoPoint)
    toolbox.register("mutate", tools.mutFlipBit, indpb=0.05)
    toolbox.register("select", tools.selTournament, tournsize=3)

    pop   = toolbox.population(n=50)
    hof   = tools.HallOfFame(1)
    stats = tools.Statistics(lambda ind: ind.fitness.values)
    stats.register("avg", numpy.mean)
    stats.register("std", numpy.std)
    stats.register("min", numpy.min)
    stats.register("max", numpy.max)

    pop, log = algorithms.eaSimple(pop, toolbox, cxpb=0.5, mutpb=0.2, ngen=30, 
                                   stats=stats, halloffame=hof, verbose=False) # change to verbose=True to see evolution table
    return f"deap test >>> {hof[0]}"