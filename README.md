# Hydrolex

### SPH Fluid Simulation
The SPH fluid simulation fundamentally entails **solving the Navier-Stokes equations numerically through the SPH method**.
To ensure the uniqueness of the solution, initial conditions are necessary, potentially encompassing the fluid's initial configuration (initial positions of each particle), initial velocities of the particles, positional constraints (spatial boundaries), or velocity constraints, etc.
Thus, SPH simulation of fluids constitutes a **Mixed Initial-Boundary Value Problem**.
For simulating a fluid with an initial shape of $x(0)$ and an initial velocity of $v_0$ , the process to solve $V(t+\Delta{t})$ can be decomposed into several sub-steps:

1. Solving $\frac{D_v}{D_t}=\frac{\mu}{\rho}\nabla^2v+\frac{1}{\rho}f_{ext}$ to update $v$ ;
2. Applying $\frac{D_{\rho}}{D_t}=0$ to solve for $\nabla p$ ;
3. Solving $\frac{D_v}{D_t}=\frac{1}{\rho}\nabla p$ to update $v$ ;
4. Solving $\frac{D_x}{D_t}=v$ to update $x$ ;

By breaking down the entire simulation problem into a series of continuous sub-problems and addressing each independently, the overall complexity of the simulation is effectively reduced, allowing for varied updating methods.

### Fundamental Theory of SPH

SPH employs approximate incremental kernel functions. At any given location $r$ within space $\Omega$ , any field quantity $A(r)$ can be computed through

(1) $A(r)=\int_{\Omega}{A(r')W(r-r',h)dr'}$

where $W$ is a smooth kernel function with a compact support radius of $h$ . A numerical equivalent of this integral can be obtained through interpolation:

(2) $A_s(r)=\Sigma_j{A_jV_jW(r-r_j,h)}$

Here, $j$ represents all particles in the domain, $r_j$ the position of particle $j$ , and $A_j$ the field quantity at $r_j$ . $V_j$ is the volume occupied by particle $j$ in the domain, with volume related to mass and mass density as follows:

(3) $V=\frac{m}{\rho}$

where $m$ is the particle mass, and $\rho$ is its mass density. Substituting (3) into (2) yields the fundamental formula of the SPH method:

(4) $A_s(r)=\Sigma_j{A_j\frac{m_j}{\rho_j}W(r-r_j,h)}$

This formula is applicable for approximating any continuous field quantity at any location within the domain.

When the kernel function meets the second-order differentiability condition, the SPH method can interpolate first-order (gradient) and second-order (Laplacian) differential operators of the field quantity.
Since $A_j\frac{m_j}{\rho_j}$ is independent of the differential variable, it can be treated as a constant product, meaning the differential operators only affect the SPH kernel function.
The gradient of the SPH formula is:
$$\nabla A_s(r)=\sum_{j}A_j\frac{m_j}{\rho_j}\nabla W(r-r_j,h)$$
However, direct discretization of this first-order SPH differential operator can lead to significant errors, causing simulation instability. Hence, two widely used discretization formulas for the SPH first-order differential operator emerged:

1. Difference Formula: Also known as the differential gradient estimate, it offers higher numerical accuracy but can lead to momentum loss (non-conservation of momentum), causing simulation instability.
   $$\nabla A_i(r)=\sum_{j}(A_j-A_i)\frac{m_j}{\rho_j}\nabla W(r-r_j,h)$$
2. Symmetric Formula: This formula lacks zeroth and first-order accuracy but ensures the conservation of linear and angular momentum, offering robustness.
   $$\nabla A_s(r)=\rho_i\sum_{j}m_j(\frac{A_i}{\rho_i^2}+\frac{A_j}{\rho_j^2})\nabla W(r-r_j,h)$$

In practice, the Difference Formula is commonly used for discretizing indirect first-order differential operators, such as velocity divergence, while the Symmetric Formula is employed for physical forces and impulses that affect particle positions.

The Laplacian operator of the SPH formula is:
$$\nabla^2 A_s(r)=\sum_{j}A_j\frac{m_j}{\rho_j}\nabla^2 W(r-r_j,h)$$

Similarly, direct discretization of the second-order SPH differential operator can lead to significant errors. By using the first-order derivative of the kernel function and performing operations similar to finite differences (dividing by the distance between particles), second-order differential discretization can be achieved:
$$\nabla^2 A_i(r)=-\sum_{j}(A_j-A_i)\frac{2\||\nabla W(r-r_j,h)\||}{\||r_{ij}\||}$$

However, the forces derived from this second-order differential discretization do not conserve momentum.

#### Kernel Function

The smooth kernel function is of paramount importance to SPH, controlling the extent to which the field quantity $A(r)$ at a certain location is influenced by its neighborhood.
Different field quantities in SPH should apply different kernel functions.
A kernel function must satisfy five conditions:

1. Normalization Condition: $$\int_\Omega W(r,h)dr=1$$

2. Dirac Delta Condition: The delta distribution is the situation of the Gaussian distribution $(0,\delta^2)$ as $\delta \to 0$ , i.e.,

$$
\delta(r)=
\begin{cases}
\infty & \text{r=0} \\
0 & \text{otherwise}
\end{cases}
$$

$$\lim_{h \to 0}W(r,h)=\delta(r)$$

3. Positivity Condition: Ensures that the field quantity distribution has no negative values. $$W(r,h) \geq 0$$

4. Symmetry Condition: $$W(r,h)=W(-r,h)$$

5. Compact Support Condition: $$W(r,h)=0 \text{ when } \||r\|| \geq h$$


### Neighbor Particle Search
Several critical steps in the SPH solution process require considering the contributions of neighboring particles within the kernel radius, necessitating the search for neighbor particles for each particle in the entire problem domain space.
Common neighbor particle search methods include global search, spatial uniform grid, and spatial multidimensional tree.

**Global Search:**
The most straightforward method involves globally searching particles and calculating the distance between two particles to determine if they are within the kernel radius.
The complexity of global search is directly related to the total number of particles in the problem domain, with a complexity of $O(n^2)$ when the total particle count is $n$ .

**Spatial Uniform Partitioning (Uniform Grid):**
This method uses a spatial uniform grid for neighbor particle search, with an algorithm complexity of: establishing grid-particle mapping $O(n)$, and searching for neighbor particles $O(1)$ .
Spatial hashing uses a hash table to store grid information. For a grid with coordinates $X_{grid}=(x, y, z)$ , the hash function to calculate its key value is:
$$hash(X_{grid})=(xP_1)XOR(yP_2)XOR(zP_3)mod N$$
where $P_1=73856093$ , $P_2=19349663$, $P_3=83492791$ are three large prime numbers, $N$ is the total number of grids, and $XOR$ is the bitwise exclusive OR operator. Based on the particle coordinates $pos_i=(x, y, z)$ , the grid coordinates to which the particle belongs can be calculated:
$pos_{grid}=(\frac{x}{L_x}, \frac{y}{L_y}, \frac{z}{L_z})$
where $L_x$ , $L_y$ , $L_z$ are the grid scales in the $x$ , $y$ , and $z$ dimensions, respectively.

When using spatial uniform partitioning for neighbor search, the surrounding 26 grid coordinates can be determined based on the current particle's grid coordinates. After calculating these 27 grids, the particles in the grids are queried in the hash table to determine if they are neighbors of the current particle.
In other words, compared to global search, spatial uniform partitioning narrows the search range to the particle's own and the surrounding 27 grids, significantly reducing the computational load of the search.

### Density Calculation

The particles considered in the density calculation include neighbor particles and the particle itself.
The density calculation formula is:

$$\rho_i=m\Sigma{W_{poly6}}$$

The density uses the smooth kernel:

$$
W_{poly6}=
\begin{cases}
\frac{315}{64\pi h^9}(h^2-r^2)^3 & \text{if } r < h \\
0 & \text{if } r \geq h
\end{cases}
$$

### Pressure Calculation

Pressure arises due to differences in density. The method of solving pressure is a key characteristic that distinguishes SPH methods.
Current SPH methods can be broadly divided into two types:
- One is the **Weakly Compressible SPH (WCSPH)**, which calculates pressure through a **State Equation**.
- The other is incompressible SPH that solves the **Pressure Poisson Equation**, including methods like PCISPH, IISPH, DFSPH, etc.

WCSPH: WCPSH is a non-iterative pressure solver that calculates pressure through a state equation.
The steps to determine pressure only require the current particle density, and various state equations can be used:

1. **Ideal Gas State Equation:** $$p_i=k(\rho_i-\rho_0)$$
   This method applies pressure linearly based on the stiffness system $k$ and the difference between the particle $i$ 's density $\rho_i$ and the static density $\rho_0$ .
   It is evident that the greater the difference between the current density and the static density, the greater the applied pressure.

2. **Tait State Equation:**
   $$p_i=k((\frac{\rho_i}{\rho_0})^\gamma-1)$$
   Typically, $\gamma=7$ is chosen.

### Pressure Force Calculation

After determining the pressure at particle $i$ , the pressure force experienced by particle $i$ can be calculated.
The pressure force arises from surrounding particles due to pressure differences.
A straightforward understanding is: fluids always flow from high-pressure areas to low-pressure areas.
The calculation formula is:

$$f_i^{press}=-\rho_i\Sigma{m_j(\frac{p_i}{\rho_i^2}+\frac{p_j}{\rho_j^2})\Delta{W_{spiky}}}$$

The pressure calculation can use the Spiky gradient kernel, whose gradient is:

$$
\Delta{W_{spiky}}=
\begin{cases}
-\frac{45}{\pi h^6}(h-r)^2e_r & \text{if } r < h \\
0 & \text{if } r \geq h
\end{cases}
$$

where $e_r$ represents the direction vector pointing from particle $i$ to neighbor particle $j$.

### Viscous Force Calculation

Viscous force arises from the relative movement between fluid elements and can be understood as the dynamic friction force between particles.
The calculation formula is:

$$f_i^{visco}=\frac{\mu}{\rho_i}\Sigma{m_j(u_j-u_i)\Delta^2{W_{visco}}}$$

The viscous force calculation uses the viscous force smooth kernel, whose Laplacian operator is:

$$
\Delta^2{W_{visco}}=
\begin{cases}
\frac{45}{\pi h^6}(h-r) & \text{if } r < h \\
0 & \text{if } r \geq h
\end{cases}
$$

### Gravity Calculation

Gravity calculation is based on the gravitational acceleration $g$ :

$$f_i^{gravity}=\rho_ig$$

### Time Integration

As the entire SPH simulation problem is split into multiple sub-problems in the time domain, numerical integration over time is required to obtain the solution at each time point.
**Explicit Integration** is straightforward and effective, while **Implicit Integration** has its advantages.
However, the most widely used is **Semi-Implicit Integration**.
The goal of graphics simulation is to achieve stable and reliable simulation in a resource-efficient manner, with numerical accuracy often being secondary.

The choice of **time step** is crucial for the stability and accuracy of the simulation, as well as its efficiency.
Larger time steps can enhance simulation efficiency but may lead to a decrease in numerical accuracy or even instability.
Smaller time steps can improve numerical accuracy but sacrifice simulation efficiency.
Graphics simulation necessitates maintaining small time steps to ensure stable and accurate simulation while choosing as large a time step as possible to enhance simulation efficiency.
