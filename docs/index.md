# Paranormal activity detector: the method description
> **ƒ** &nbsp;RD AAOW FDL; 18.03.2023; 18:46



### Page contents

- [General information](#general-information)
- [How does it work](#how-does-it-work)
- [Description of the method](#description-of-the-method)
- [Conclusion](#conclusion)

---

### Background of the project

Let’s start from afar. It is known that some people (in fact, many) have abilities that go beyond what science
has been able to define as the norm. They have many variations and manifestations. One of them is the influence
on the surrounding objects and phenomena without “direct” contact (without touching and commands). It cannot
be called reliably (consistently) proven. However, this is more than likely.

Science tells us that we constantly live in streams of electromagnetic emission of various nature and strength.
The gamma, X-ray and ultraviolet light of the Sun, as well as the radio waves of the WiFi router and NFC
transmitter, are on this list. All of them can influence us. And there is a theory that the opposite effect
is also possible. However, why the theory: electroencephalography is a real medical test of brain activity,
and the hammerhead shark quite realistically searches for its prey in complete darkness.

To put it simply (may physicists and doctors forgive us), brain activity is capable of introducing distortions
into electromagnetic fields, “giving out” itself and its power. And this can be registered. However, we want
to go a little further...

There is an *unverified theory* that perturbations introduced by people (primarily through strong emotions)
into the conditionally uniformly moving (artificial distortions are ignored) emission streams can, among
other things, manifest themselves in “failures” in the operation of the RNG, the principle of action most
of which are based on the registration of these emissions.

**Random number generators** are devices that allow you to form sequences of numbers, each of the next of which
cannot be predicted from the previous ones. “Failures” in work of the RNG lead to a distortion of the probability
distribution in the sequences, which is exactly recorded according to this theory. It says that this is exactly
what happened with some real generators (we are talking, first of all, about casinos where they are widespread)
before the events of September 11, as well as the tsunami of 2004 and 2011.

Let’s decipher: if the generator makes numbers from 1 to 100 with the same probability of occurrence of any
of them (1%), then the above errors “deform” this distribution, causing, f. e., a 3% probability of occurrence
of the number 54. And registration of the obtained values and calculating the average instead of the conditional
standard of 50.5±0.5 will give something like 51.6±0.9 or 49.8±0.7 during a statistically significant period
of time.

This is the theory. And we seem to know how to test it. At the moment, we’ve already developed an application
for Android, which will allow us to conduct an interesting experiment. We’ll try to ***find out some forms
of paranormal activity***. And ***do it in an almost scientific way***. Even if nothing comes of it, this
will also be the result – one else theory will be removed. In any case, we hope for your participation
and support. After all, this absolutely safe test can give completely unpredictable results.



---

### How does it work

Let’s say we want to choose between purchasing a *laptop*, *smartphone*, *tablet*, and *desktop*. Let’s run
Make decision and specify the devices to be compared. There should obviously be at least two points for comparison.

<center><img src="/MakeDecision/img/V_EN_01.png" width="250" /></center>

Note that if necessary, all steps can be repeated again at any time using the `↺` button, and the transition
to the next step (hereinafter) is performed with the `▶` button.

Next, we indicate the criteria by which we will compare devices. Let’s say it will be *price*,
*modernability* (possibility of “overclocking”, replacement of components, etc.), *power* and *convenience*
(the ability to take with you, use outside, etc.).

<center><img src="/MakeDecision/img/V_EN_02.png" width="250" /></center>

At the same time, it is important to indicate the **“costs”** of these criteria when making a decision. It means some abstract number,
like a scale from 1 to 100, which would be larger for a more significant parameter.

Let’s say we value *convenience* and *power* more than the ability to change the initial configuration and price in the store.
The picture above shows this using sliders.

On the following screens (according to the number of comparison criteria), the program will prompt you to specify ratings for devices
for each criterion. Note that these are exactly estimates. Those, the product with a higher price in our case will be
have a lower score, because high cost is a less successful solution (however, this is not always the case).

<center><img src="/MakeDecision/img/V_EN_03.png" width="250" /></center>

These are, of course, controversial points. But we entered such data based on our own experience. Ideally, these estimates should
produce on the basis of literature analysis, descriptions, examinations, reviews and comments. However, for less critical
solutions may be enough good advice.

On the last screen, we get a mathematically sound answer: we need a *tablet PC*.

<center><img src="/MakeDecision/img/V_EN_07.png" width="250" /></center>

This, in general, meets our needs. Although, strictly speaking, the smartphone is not far behind in the total coefficient
and can also be considered as a solution.

Note that the app supports up to 10 elements and up to 10 criteria, i.e. allows you to take much more
complex and multifactorial decisions than in this example. And this is quite simple, because the mathematical apparatus
solutions aren’t evident to the user.

At the end of the procedure, the program can be returned to its initial state using the `↺` and `▶` buttons. Object names
and criteria are saved until they are changed manually or reset with the `✗` button.

---

### Description of the method

After the user fills lists of elements for comparison and criteria with their values, from the resulting vector
ratings, a comparison matrix is created. To do this, duplication of the original vector is performed until the matrix
doesn’t become square. After that, each column is divided into that element whose number is equal to the number
columns in a matrix. As a result, in the matrix on the main diagonal, all elements become equal to one
(this process is known as *matrix normalization*).

<center><img src="/MakeDecision/img/Vector.png" /></center>

Similarly, the vectors of evaluations of elements for each criterion are set and processed.

The final scores of the elements are obtained as follows:

1. For all normalized matrices (both criteria and elements), vectors of mean harmonics are compiled:
each element of the vector is equal to the product of the elements of the corresponding row of the matrix, raised to a power,
reciprocal of the number of elements in the row.

2. Then the matrices are multiplied by these vectors.

<center><img src="/MakeDecision/img/Matrix.png" /></center>

3. Next, the resulting cost vectors of elements according to different criteria are glued into a matrix in the order
in which these criteria were declared.

4. Finally, this matrix is multiplied by the criterion cost vector.

The resulting vector will be the result of the method. The largest number in it will indicate the “best” element
under given conditions.

---

### Conclusion

So, the non-linear method of analyzing hierarchies, with the correct indication of the initial data, may turn out to be
indispensable in a seemingly insoluble choice. We strongly recommend that you try out this tool,
when the question of “either-or” will rise especially sharply. However, it can also be useful in professional activities,
because it uses a proven mathematical apparatus, which means the reliability and scientific nature of the solutions obtained.
